using System.Net.Sockets;
using System.Security.Cryptography;

namespace Endless.Apple.Protocol;

/// <summary>A bounded, explicit sign-in interoperability test. Does not enter the world.</summary>
public static class RechargedLogin
{
    public static async Task<SessionCheckResult> TestAsync(ServerProfile profile, string identity,
        char[] username, char[] password, CancellationToken token, IProgress<SessionCheckResult>? progress = null, bool selectSingleCharacter = false, Task<int>? characterSelection = null, Func<Stream, RechargedSession, RechargedWelcome, CancellationToken, Task<SessionCheckResult>>? worldSession = null)
    {
        try
        {
            if (profile.Dialect != ProtocolDialect.Recharged) throw new ArgumentException("Requires Recharged profile.");
            using var socket = new TcpClient();
            await socket.ConnectAsync(profile.Host, profile.Port, token);
            return await TestStreamAsync(socket.GetStream(), profile, identity, username, password, token, progress, selectSingleCharacter, characterSelection, worldSession);
        }
        finally { Array.Clear(username); Array.Clear(password); }
    }

    internal static async Task<SessionCheckResult> TestStreamAsync(Stream stream, ServerProfile profile,
        string identity, char[] username, char[] password, CancellationToken token, IProgress<SessionCheckResult>? progress = null, bool selectSingleCharacter = false, Task<int>? characterSelection = null, Func<Stream, RechargedSession, RechargedWelcome, CancellationToken, Task<SessionCheckResult>>? worldSession = null)
    {
        var sent = false;
        SessionCheckResult? accepted = null;
        var acknowledged = false;
        var selectedId = 0;
        Task<byte[]>? pendingFrame = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(25));
        token = timeout.Token;
        try
        {
            // Validate locally before any network writes; discard the validation buffers.
            CryptographicOperations.ZeroMemory(RechargedCredentials.Encode(username, 123));
            CryptographicOperations.ZeroMemory(RechargedCredentials.Encode(password, 0));
            var challenge = RandomNumberGenerator.GetInt32(1, 1_000_000);
            await stream.WriteAsync(Handshake.Request(profile, challenge, identity), token);
            var initial = await EoWire.ReadFrameAsync(stream, token);
            HandshakeResult result;
            try { result = Handshake.Parse(initial, challenge, profile.Dialect); }
            catch (InvalidDataException)
            {
                return new("invalid-initialization", "The server returned an unexpected initialization response. Credentials were not sent. Please wait before trying again.", false, null, null, initial.Length);
            }
            if (result.Status != "init-received")
                return new(result.Status, "The server did not accept initialization. Credentials were not sent.", false, null, null, initial.Length);
            var session = new RechargedSession(initial, challenge);
            await stream.WriteAsync(session.Accept(), token);
            acknowledged = true;
            for (var packets = 0; packets < 2048; packets++)
            {
                pendingFrame ??= EoWire.ReadFrameAsync(stream, token);
                if (accepted != null && selectedId == 0 && characterSelection != null
                    && (characterSelection.IsCompleted || await Task.WhenAny(pendingFrame, characterSelection) == characterSelection))
                {
                    var id = await characterSelection;
                    if (accepted.Characters == null || !accepted.Characters.Any(c => c.Id == id))
                        throw new InvalidDataException("Select a character returned by this session.");
                    selectedId = id;
                    await stream.WriteAsync(session.SelectCharacter(id, RandomNumberGenerator.GetInt32(2, 200)), token);
                }
                var frame = await pendingFrame;
                pendingFrame = null;
                var decoded = session.Decode(frame);
                try
                {
                    if (RechargedSession.IsSynchronizationRequest(decoded))
                    {
                        await stream.WriteAsync(session.Synchronize(), token);
                        if (!sent)
                        {
                            var login = session.Login(username, password);
                            try { await stream.WriteAsync(login, token); }
                            finally { CryptographicOperations.ZeroMemory(login); Array.Clear(username); Array.Clear(password); }
                            sent = true;
                        }
                        continue;
                    }
                    if (decoded[0] == 21 && decoded[1] == 1)
                    {
                        await stream.WriteAsync(session.AnswerKeepalive(decoded), token);
                        continue;
                    }
                    if (sent && accepted == null && RechargedLoginReply.Parse(decoded) is { } reply)
                    {
                        if (reply.Status != "login-accepted") return reply;
                        try { reply = reply with { Characters = RechargedCharacters.Parse(decoded) }; }
                        catch (InvalidDataException ex)
                        {
                            // Only structural lengths and a fixed parser error; never packet contents.
                            var lengths = new List<int>();
                            var length = 0;
                            foreach (var b in decoded.Skip(2))
                            {
                                if (b == 255) { lengths.Add(length); length = 0; }
                                else length++;
                            }
                            lengths.Add(length);
                            return reply with { Message = $"Sign-in accepted; character parsing stopped: {ex.Message} Field lengths: {string.Join(",", lengths)}. Connection closed." };
                        }
                        if (selectSingleCharacter)
                        {
                            if (reply.Characters?.Count != 1)
                                return reply with { Status = "selection-needs-choice", Message = "Exactly one character is required for this diagnostic." };
                            accepted = reply;
                            selectedId = reply.Characters[0].Id;
                            await stream.WriteAsync(session.SelectCharacter(reply.Characters[0].Id,
                                RandomNumberGenerator.GetInt32(2, 200)), token);
                            continue;
                        }
                        if (progress == null && characterSelection == null) return reply;
                        accepted = reply with { Message = "Connected. Choose a character to check world loading." };
                        progress?.Report(accepted);
                        timeout.CancelAfter(TimeSpan.FromMinutes(5));
                        continue;
                    }
                    if (accepted != null && selectedId != 0)
                    {
                        // Current handler 0x592626 reads a one-byte result; 1 takes the welcome path.
                        var confirmed = decoded.Length >= 3 && decoded[0] == 2 && decoded[1] == 30
                            && EoWire.Number(decoded.AsSpan(2, 1)) == 1;
                        var welcome = confirmed ? RechargedWelcome.Parse(decoded, selectedId) : null;
                        if (welcome != null && worldSession != null)
                        {
                            timeout.CancelAfter(TimeSpan.FromMinutes(5));
                            var worldResult = await worldSession(stream,session,welcome,token);
                            return worldResult with { Characters = accepted.Characters, Welcome = welcome };
                        }
                        return new(confirmed ? "selection-accepted" : "selection-response",
                            confirmed ? $"Character selected. Map {welcome!.MapId} received; world loading is still being implemented. Connection closed."
                                : "Received a response to character selection; world entry is not yet verified.",
                            true, decoded[0], decoded[1], decoded.Length) { Characters = accepted.Characters, Welcome = welcome };
                    }
                    return new(sent ? "login-response" : "pre-login-response",
                        sent ? "The server replied after sign-in. This response still needs decoding; login success is not yet confirmed. The test connection is now closed."
                             : "An unsupported response arrived before sign-in. Credentials were not sent.",
                        true, decoded[0], decoded[1], decoded.Length);
                }
                finally { CryptographicOperations.ZeroMemory(decoded); CryptographicOperations.ZeroMemory(frame); }
            }
            return new("packet-limit", "The connection reached its packet limit and stopped.", acknowledged, null, null, 0);
        }
        catch (EndOfStreamException)
        {
            return new("login-closed", accepted != null ? "The authenticated connection closed. Sign in again to reconnect." : sent ? "The server closed the connection after sign-in. This does not establish that the password was incorrect."
                : "The connection closed before credentials were sent.", acknowledged, null, null, 0);
        }
        finally
        {
            timeout.Cancel();
            if (pendingFrame != null)
            {
                try { await pendingFrame; } catch (Exception) { /* Observe canceled outstanding read. */ }
            }
            Array.Clear(username); Array.Clear(password);
        }
    }
}
