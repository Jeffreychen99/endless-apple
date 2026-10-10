using System.Net.Sockets;
using System.Security.Cryptography;

namespace Endless.Apple.Protocol;

/// <summary>0.4.57c pre-login session framing; see docs/INIT-0457.md.</summary>
public sealed class RechargedSession
{
    private readonly byte credentialParameter;
    private int incoming;
    private int outgoing;
    private int sequenceStart;
    private readonly int player;
    public int PlayerId => player;
    private int counter;

    public RechargedSession(byte[] initialization, int challenge)
    {
        if (Handshake.Parse(initialization, challenge, ProtocolDialect.Recharged).Status != "init-received")
            throw new InvalidDataException("A current initialization response is required.");
        credentialParameter = unchecked((byte)(challenge % 250 + 2 + initialization[14] + 130));
        incoming = EoWire.Number(initialization.AsSpan(5, 2));
        outgoing = EoWire.Number(initialization.AsSpan(7, 2)) + challenge % 11;
        sequenceStart = initialization[3] + initialization[4] - 11;
        player = EoWire.Number(initialization.AsSpan(9, 2));
        if (sequenceStart < 0 || incoming == 0 || outgoing == 0)
            throw new InvalidDataException("Invalid session parameters.");
    }

    public static byte[] Transform(ReadOnlySpan<byte> body, int parameter, bool encode)
    {
        var output = body.ToArray();
        for (var i = 0; i < output.Length; i++)
            output[i] = unchecked((byte)(output[i] + (encode ? 1 : -1) * (parameter + i + 4)));
        return output;
    }

    public byte[] Accept()
    {
        // Official encoder emits the low two base-253 digits for these values.
        byte[] payload = [.. EoWire.Number(outgoing % 64009, 2), .. EoWire.Number(incoming, 2), .. EoWire.Number(player, 2)];
        return Packet(3, 1, payload);
    }

    public static bool IsSynchronizationRequest(ReadOnlySpan<byte> body) =>
        body.Length == 3 && body[0] == 244 && body[1] == 3;

    // Official handler 0x508fab calls 0x5052bc: Login/Syn4 with ASCII "k".
    public byte[] Synchronize() => Packet(244, 3, [(byte)'k']);

    public byte[] AnswerKeepalive(ReadOnlySpan<byte> body)
    {
        // Connection/Ping: divisor char, dividend int, trailing char (unused).
        if (body.Length != 8 || body[0] != 21 || body[1] != 1)
            throw new InvalidDataException("Unexpected keepalive layout.");
        var divisor = EoWire.Number(body.Slice(2, 1));
        var dividend = EoWire.Number(body.Slice(3, 4));
        _ = EoWire.Number(body.Slice(7, 1));
        if (divisor == 0 || dividend < 12)
            throw new InvalidDataException("Invalid keepalive parameters.");
        var nextSequence = (dividend - 12) / divisor - divisor;
        if (nextSequence < 0) throw new InvalidDataException("Invalid keepalive sequence.");
        sequenceStart = nextSequence;
        return Packet(241, 1, [(byte)'k']);
    }

    private byte[] Packet(byte action, byte family, byte[] payload)
    {
        counter = (counter + 1) % 10;
        byte[] body = [action, family, .. EoWire.Number((sequenceStart + counter) % 253, 1), .. payload];
        try { return EoWire.Frame(Transform(body, outgoing, true)); }
        finally { CryptographicOperations.ZeroMemory(body); }
    }

    // Caller owns and clears the character buffers. No credentials enter diagnostics.
    public byte[] Login(ReadOnlySpan<char> username, ReadOnlySpan<char> password)
    {
        var account = RechargedCredentials.Encode(username, 123);
        byte[]? secret = null;
        byte[]? payload = null;
        try
        {
            secret = RechargedCredentials.Encode(password, credentialParameter);
            payload = [.. account, 255, .. secret, 255];
            return Packet(35, 2, payload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(account);
            if (secret != null) CryptographicOperations.ZeroMemory(secret);
            if (payload != null) CryptographicOperations.ZeroMemory(payload);
        }
    }

    // Official 0x4fb8fc: character ID (int), modifier (char), action 35/family 30.
    // The request uses the old key; all subsequent sends use the incremented key.
    public byte[] SelectCharacter(int characterId, int modifier)
    {
        if (characterId <= 0) throw new ArgumentOutOfRangeException(nameof(characterId));
        if (modifier is < 2 or > 199) throw new ArgumentOutOfRangeException(nameof(modifier));
        var packet = Packet(35, 30, [.. EoWire.Number(characterId, 4), .. EoWire.Number(modifier, 1)]);
        outgoing += modifier;
        return packet;
    }

    // Official 0x505d54..0x505e31: action 12/family 30, welcome session (three), character (int).
    public byte[] EnterWorld(RechargedWelcome welcome)
    {
        if (welcome.SessionId <= 0 || welcome.CharacterId <= 0) throw new InvalidDataException("Invalid world entry session.");
        return Packet(12,30,[..EoWire.Number(welcome.SessionId,3),..EoWire.Number(welcome.CharacterId,4)]);
    }

    // Official 0x591c1d..0x591eaf and 0x4f59d0. Apply only after decoding the completion packet.
    public void CompleteWorldEntry(ReadOnlySpan<byte> body)
    {
        if (body.Length != 12 || body[0] != 5 || body[1] != 30 || body[11] != 255)
            throw new InvalidDataException("Unexpected world-entry completion.");
        var first = EoWire.Number(body.Slice(2,1));
        var second = EoWire.Number(body.Slice(3,1));
        _ = EoWire.Number(body.Slice(4,3));
        _ = EoWire.Number(body.Slice(7,3));
        _ = EoWire.Number(body.Slice(10,1));
        incoming += ((first+second)&255) + outgoing%13;
    }

    // Official normal-floor walk builder 0x4fc180, called by 0x4e5b27.
    // Official 0x505e5c, invoked by 0x59237a after world completion.
    public byte[] ConfirmWorld() => Packet(240,30,[1,5,58,1,1]);

    // Official Ctrl/space path 0x47ced2 -> 0x4e4588 -> 0x4e45a0;
    // basic attack (argument zero) calls 0x4fdc9c with facing direction.
    public byte[] Attack(int direction)
    {
        if (direction is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(direction));
        return Packet(35,102,EoWire.Number(direction,1));
    }

    // Official 0x4e5b44 -> 0x4fd650: independent facing, one direction char.
    // Used by blocked player/NPC movement branches (0x4e5783 / 0x4e5830).
    public byte[] Face(int direction)
    {
        if(direction is < 0 or > 3)throw new ArgumentOutOfRangeException(nameof(direction));
        return Packet(21,204,EoWire.Number(direction,1));
    }

    public byte[] Walk(int direction, int x, int y)
    {
        if (direction is < 0 or > 3 || x is < 0 or > 249 || y is < 0 or > 249)
            throw new ArgumentOutOfRangeException(nameof(direction));
        return Packet(21,206,[..EoWire.Number(direction,1),..EoWire.Number(x,1),..EoWire.Number(y,1)]);
    }

    // Official 0x50ce20..0x50d146: mode char 1, missing player shorts,
    // FF separator, missing NPC shorts. There is no trailing separator.
    public byte[] RequestAppearances(IReadOnlyList<int> players, IReadOnlyList<int> npcs)
    {
        if (players.Count > 1024 || npcs.Count > 1024 || players.Count + npcs.Count == 0)
            throw new ArgumentException("Invalid appearance request size.");
        var payload = new List<byte> { 2 };
        foreach (var id in players) payload.AddRange(EoWire.Number(id,2));
        payload.Add(255);
        foreach (var id in npcs) payload.AddRange(EoWire.Number(id,2));
        return Packet(1,218,payload.ToArray());
    }

    public byte[] Decode(byte[] body) => Transform(body, incoming, false);

    public static async Task<SessionCheckResult> CheckAsync(ServerProfile profile, string identity, CancellationToken token, bool watch = false, IProgress<SessionCheckResult>? progress = null)
    {
        if (profile.Dialect != ProtocolDialect.Recharged) throw new ArgumentException("Requires Recharged profile.");
        using var socket = new TcpClient();
        await socket.ConnectAsync(profile.Host, profile.Port, token);
        var stream = socket.GetStream();
        return await CheckStreamAsync(stream, profile, identity, token, watch, progress);
    }

    internal static async Task<SessionCheckResult> CheckStreamAsync(Stream stream, ServerProfile profile,
        string identity, CancellationToken token, bool watch = false, IProgress<SessionCheckResult>? progress = null)
    {
        var challenge = RandomNumberGenerator.GetInt32(1, 1_000_000);
        await stream.WriteAsync(Handshake.Request(profile, challenge, identity), token);
        var initial = await EoWire.ReadFrameAsync(stream, token);
        var result = Handshake.Parse(initial, challenge, profile.Dialect);
        if (result.Status != "init-received") return new(result.Status, result.Message, false, null, null, 0);
        var session = new RechargedSession(initial, challenge);
        await stream.WriteAsync(session.Accept(), token);
        // Bounded pre-login observation. No account query or gameplay command.
        using var observation = CancellationTokenSource.CreateLinkedTokenSource(token);
        observation.CancelAfter(TimeSpan.FromSeconds(watch ? 45 : 20));
        var synchronizationReplies = 0;
        var keepaliveReplies = 0;
        try
        {
            for (var packets = 0; packets < 64; packets++)
            {
                var frame = await EoWire.ReadFrameAsync(stream, observation.Token);
                if (frame[0] == 255 && frame[1] == 255)
                    return new("session-not-verified", "Server returned another initialization packet after acknowledgement.", true, 255, 255, frame.Length);
                var decoded = session.Decode(frame);
                if (IsSynchronizationRequest(decoded))
                {
                    await stream.WriteAsync(session.Synchronize(), observation.Token);
                    synchronizationReplies++;
                    if (watch)
                    {
                        if (synchronizationReplies == 1)
                            progress?.Report(new("observing-session", "Server synchronization received. Checking that the connection stays open for 45 seconds…", true, 244, 3, decoded.Length));
                        continue;
                    }
                    return new("synchronization-replied", "Received the server's pre-login synchronization request and sent its reply. Sign-in is not implemented yet.",
                        true, decoded[0], decoded[1], decoded.Length);
                }
                if (decoded[0] == 21 && decoded[1] == 1)
                {
                    await stream.WriteAsync(session.AnswerKeepalive(decoded), observation.Token);
                    keepaliveReplies++;
                    if (watch)
                    {
                        progress?.Report(new("observing-session", "Server keepalive received and answered. Continuing the connection check…", true, 21, 1, decoded.Length));
                        continue;
                    }
                    return new("keepalive-replied", "Received and answered the server keepalive.", true, 21, 1, decoded.Length);
                }
                return new("session-response", "Received a packet after connection acknowledgement. Authentication remains unimplemented.",
                    true, decoded[0], decoded[1], decoded.Length);
            }
            return new("packet-limit", "Stopped after 64 packets.", true, null, null, 0);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            if (watch && synchronizationReplies > 0)
                return new("session-observed", $"Connection stayed open for 45 seconds; answered {synchronizationReplies} synchronization requests and {keepaliveReplies} keepalives. Authentication is not implemented.", true, null, null, 0);
            return new("session-unconfirmed", $"Acknowledgement sent; no recognized synchronization arrived within {(watch ? 45 : 20)} seconds.", true, null, null, 0);
        }
        catch (EndOfStreamException)
        {
            return new("session-closed", "Server closed the connection after acknowledgement.", true, null, null, 0);
        }
    }
}

public sealed record SessionCheckResult(string Status, string Message, bool AcknowledgementSent, int? Action, int? Family, int ResponseBytes)
{
    public IReadOnlyList<RechargedCharacter>? Characters { get; init; }
    public RechargedWelcome? Welcome { get; init; }
}
