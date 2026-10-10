namespace Endless.Apple.Protocol;

/// <summary>The verified prefix of the current welcome response, not a complete world snapshot.</summary>
public sealed record RechargedWelcome(int CharacterId, int MapId, int MapRevisionLow,
    int MapRevisionHigh, int MapFileSize)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public int SessionId { get; init; }

    public static RechargedWelcome Parse(ReadOnlySpan<byte> body, int selectedId)
    {
        // Official handler 0x592626..0x592b73; offsets below exclude opcode bytes.
        if (body.Length < 21 || body[0] != 2 || body[1] != 30)
            throw new InvalidDataException("Truncated or unexpected welcome response.");
        var payload = body[2..];
        if (EoWire.Number(payload[..1]) != 1)
            throw new InvalidDataException("Character selection was not accepted.");
        _ = EoWire.Number(payload.Slice(2, 1)); // Resource encoding flag.
        var sessionId = EoWire.Number(payload.Slice(3, 2)); // Session field; never expose in diagnostics.
        var characterId = EoWire.Number(payload.Slice(5, 4));
        var mapId = EoWire.Number(payload.Slice(9, 2));
        _ = EoWire.Number(payload.Slice(11, 1));
        var revisionLow = EoWire.Number(payload.Slice(12, 2));
        var revisionHigh = EoWire.Number(payload.Slice(14, 2));
        var fileSize = EoWire.Number(payload.Slice(16, 3));
        if (characterId <= 0 || characterId != selectedId || mapId <= 0)
            throw new InvalidDataException("Welcome response does not match the selected character or map.");
        return new(characterId, mapId, revisionLow, revisionHigh, fileSize) { SessionId = sessionId };
    }
}
