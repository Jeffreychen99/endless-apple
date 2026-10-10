namespace Endless.Apple.Protocol;

/// <summary>Account/Agree handler at 0x5970e4 in the 0.4.57c client.</summary>
public static class RechargedLoginReply
{
    public static SessionCheckResult? Parse(ReadOnlySpan<byte> body)
    {
        if (body.Length < 2 || body[0] != 3 || body[1] != 2) return null;
        if (body.Length < 4) throw new InvalidDataException("Truncated sign-in result.");
        var code = EoWire.Number(body.Slice(2, 2));
        if (code != 1)
            return new("login-unrecognized-result", $"The server returned sign-in result {code}. Its meaning is not implemented; authentication is not confirmed.", true, 3, 2, body.Length);
        // The accepted branch reads a 13-byte numeric header before its first FF.
        var payload = body[2..];
        var separator = payload.IndexOf((byte)255);
        if (separator < 13) throw new InvalidDataException("Truncated accepted sign-in header.");
        _ = EoWire.Number(payload.Slice(2, 2));
        _ = EoWire.Number(payload.Slice(4, 2));
        _ = EoWire.Number(payload.Slice(6, 1));
        _ = EoWire.Number(payload.Slice(7, 2));
        _ = EoWire.Number(payload.Slice(9, 2));
        _ = EoWire.Number(payload.Slice(11, 2));
        return new("login-accepted", "The server accepted sign-in. Character selection and world entry are not implemented yet; this test connection is now closed.", true, 3, 2, body.Length);
    }
}
