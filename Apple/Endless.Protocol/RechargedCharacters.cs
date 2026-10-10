using System.Text;

namespace Endless.Apple.Protocol;

public sealed record RechargedCharacter(int Id, string Name)
{
    public int? Level { get; init; }
    public WorldActor? Appearance { get; init; }
}

public static class RechargedCharacters
{
    // 0x5975ab passes the remainder after the account header to 0x5a17ac.
    // The list begins with count + FF, then name/title/guild/stats segments.
    public static IReadOnlyList<RechargedCharacter> Parse(ReadOnlySpan<byte> body)
    {
        if (RechargedLoginReply.Parse(body)?.Status != "login-accepted")
            throw new InvalidDataException("An accepted account reply is required.");
        var payload = body[2..];
        var remaining = payload[(payload.IndexOf((byte)255) + 1)..];
        var countHeader = Take(ref remaining);
        if (countHeader.Length < 1) throw new InvalidDataException("Unsupported character count layout.");
        var count = EoWire.Number(countHeader[..1]);
        // Official 0x5a235f bounds iteration by both count and three slots.
        // Its delimiter reader returns empty fields after EOF and skips absent slots.
        count = Math.Min(count, 3);
        var characters = new List<RechargedCharacter>(count);
        for (var i = 0; i < count; i++)
        {
            if (remaining.IsEmpty) break;
            var name = Take(ref remaining);
            _ = Take(ref remaining); // Title, not needed for selection.
            _ = Take(ref remaining); // Guild, not needed for selection.
            var stats = Take(ref remaining, allowEnd: true);
            if (name.Length is < 1 or > 32 || stats.Length < 26)
                throw new InvalidDataException("Incomplete character record.");
            foreach (var b in name)
                if (b is < 32 or > 126) throw new InvalidDataException("Unsupported character name encoding.");
            var id = EoWire.Number(stats[..4]);
            if (id == 0 || characters.Any(c => c.Id == id)) throw new InvalidDataException("Invalid character identifier.");
            var characterName = Encoding.ASCII.GetString(name);
            WorldActor? appearance = null;
            // Official 0x5a1b93..0x5a1d17: gender, hairstyle, color, skin.
            // 0x5a1f01..0x5a22e7: nine equipment shorts, beginning at byte 13.
            if (stats.Length >= 31)
            {
                var equipment = new int[9];
                for (var slot=0; slot<9; slot++) equipment[slot]=EoWire.Number(stats.Slice(13+slot*2,2));
                appearance = new WorldActor(id,characterName,0,0,0,
                    EoWire.Number(stats.Slice(6,1)),EoWire.Number(stats.Slice(9,1)),
                    EoWire.Number(stats.Slice(7,1)),EoWire.Number(stats.Slice(8,1)),0,0,equipment);
            }
            characters.Add(new(id, characterName) { Appearance=appearance, Level=stats.Length>=31 ? EoWire.Number(stats.Slice(4,2)) : null });
        }
        if (!remaining.IsEmpty) throw new InvalidDataException("Unsupported trailing character data.");
        return characters.AsReadOnly();
    }

    private static ReadOnlySpan<byte> Take(ref ReadOnlySpan<byte> remaining, bool allowEnd = false)
    {
        var delimiter = remaining.IndexOf((byte)255);
        if (delimiter < 0 && allowEnd)
        {
            var last = remaining;
            remaining = [];
            return last;
        }
        if (delimiter < 0) throw new InvalidDataException("Missing character field delimiter.");
        var field = remaining[..delimiter];
        remaining = remaining[(delimiter + 1)..];
        return field;
    }
}
