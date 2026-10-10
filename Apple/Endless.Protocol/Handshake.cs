using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Endless.Apple.Protocol;

public enum ProtocolDialect { Classic, Recharged }
public sealed record ServerProfile(string Host, int Port, int Major, int Minor, int Patch, ProtocolDialect Dialect)
{
    public static ServerProfile Official => new("game.endless-online.com", 8078, 0, 4, 57, ProtocolDialect.Recharged);
    public string Version => $"{Major}.{Minor}.{Patch}";
}
public sealed record HandshakeResult(string Status, string Message, int ReplyCode, bool ChallengeMatches,
    int ResponseBytes, string? RequiredVersion = null, int? ServerEncoding = null, int? ClientEncoding = null,
    int? ExtraIncrement = null, string? ServerMessage = null);

public static class Handshake
{
    public static byte[] Request(ServerProfile profile, int challenge, string installationId, int? initializationNoise = null)
    {
        if (installationId.Length is < 1 or > 32 || installationId.Any(c => c is < '0' or > '9'))
            throw new ArgumentException("Installation ID must be 1–32 decimal digits.");
        var recharged = profile.Dialect == ProtocolDialect.Recharged;
        var noise = initializationNoise ?? RandomNumberGenerator.GetInt32(1_000_000);
        if (noise is < 0 or >= 1_000_000) throw new ArgumentOutOfRangeException(nameof(initializationNoise));
        if (challenge < 0 || challenge >= (recharged ? 1_000_000 : 16_194_277))
            throw new ArgumentOutOfRangeException(nameof(challenge));
        // 0.4.57c writes a randomized unsigned difference first, then the real
        // challenge after marker 113. See docs/INIT-0457.md for binary evidence.
        var first = recharged ? (int)(unchecked((uint)(challenge - noise)) % 16_194_277u) : challenge;
        return EoWire.Frame([255, 255, .. EoWire.Number(first, 3),
            .. EoWire.Number(profile.Major, 1), .. EoWire.Number(profile.Minor, 1), .. EoWire.Number(profile.Patch, 1),
            .. EoWire.Number(profile.Dialect == ProtocolDialect.Recharged ? 113 : 112, 1),
            .. (recharged ? EoWire.Number(challenge, 3) : Array.Empty<byte>()),
            .. EoWire.Number(installationId.Length, 1), .. Encoding.ASCII.GetBytes(installationId)]);
    }

    public static int ChallengeHash(int challenge)
    {
        if (challenge is < 0 or > 11092110) throw new ArgumentOutOfRangeException(nameof(challenge));
        ++challenge;
        return 110905 + (challenge % 9 + 1) * ((11092004 - challenge) % ((challenge % 11 + 1) * 119)) * 119 + challenge % 2004;
    }

    public static HandshakeResult Parse(byte[] body, int challenge, ProtocolDialect dialect)
    {
        if (body.Length < 3 || body[0] != 255 || body[1] != 255)
            throw new InvalidDataException("Expected an unencoded initialization reply.");
        int code = body[2];
        if (code == 1)
        {
            if (body.Length != 6) throw new InvalidDataException("Unexpected version response layout.");
            var version = string.Join('.', body.Skip(3).Select(b => EoWire.Number(new byte[] { b })));
            return new("out-of-date", "Server requires a different client version.", code, false, body.Length, version);
        }
        if (code == 3)
        {
            // Some servers append human-readable rejection text to the older layout.
            // Display text only; never interpret it as instructions or log opaque session bytes.
            var printable = new string(body.Skip(3).Take(512).Select(b => b is >= 32 and <= 126 ? (char)b : ' ' ).ToArray()).Trim();
            var message = printable.Length >= 8 ? printable : null;
            return new("rejected", "Server rejected the connection. No further requests sent.", code, false, body.Length, ServerMessage: message);
        }
        if (code != 2) return new("unknown-reply", "Unrecognized initialization response; compatibility is unverified.", code, false, body.Length);
        var expected = dialect == ProtocolDialect.Recharged ? 15 : 12;
        if (body.Length != expected)
            return new("layout-mismatch", $"Expected {expected} bytes; received {body.Length}. Protocol work is required.", code, false, body.Length);
        var width = dialect == ProtocolDialect.Recharged ? 2 : 1;
        var server = EoWire.Number(body.AsSpan(5, width));
        var client = EoWire.Number(body.AsSpan(5 + width, width));
        // The current official client stores these three bytes as session data;
        // it no longer verifies the classic challenge hash here (0x4f480e–0x4f4841).
        if (dialect == ProtocolDialect.Recharged)
        {
            _ = EoWire.Number(body.AsSpan(11, 3)); // Validate the session field encoding.
            _ = EoWire.Number(body.AsSpan(9, 2)); // Validate player ID encoding.
            return new("init-received", "The server returned initialization data. Session setup and sign-in are not implemented yet.",
                code, false, body.Length, ServerEncoding: server, ClientEncoding: client, ExtraIncrement: body[^1]);
        }
        var hash = EoWire.Number(body.AsSpan(7 + width * 2, 3));
        var matches = hash == ChallengeHash(challenge);
        return new(matches ? "init-accepted" : "challenge-mismatch",
            matches ? "Initialization accepted. Authentication and world entry are not yet verified." : "Challenge differs from the documented algorithm. Stopped before authentication.",
            code, matches, body.Length, ServerEncoding: server, ClientEncoding: client,
            ExtraIncrement: dialect == ProtocolDialect.Recharged ? body[^1] : null);
    }

    public static async Task<HandshakeResult> ProbeAsync(ServerProfile profile, string installationId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(profile.Host) || profile.Port is < 1 or > 65535)
            throw new ArgumentException("Invalid server address.");
        var challenge = RandomNumberGenerator.GetInt32(1, profile.Dialect == ProtocolDialect.Recharged ? 1_000_000 : 11000000);
        var request = Request(profile, challenge, installationId);
        using var socket = new TcpClient();
        await socket.ConnectAsync(profile.Host, profile.Port, token);
        using var stream = socket.GetStream();
        await stream.WriteAsync(request, token);
        return Parse(await EoWire.ReadFrameAsync(stream, token), challenge, profile.Dialect);
    }
}
