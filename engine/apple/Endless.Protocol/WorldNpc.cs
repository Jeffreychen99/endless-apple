namespace Endless.Apple.Protocol;

public sealed record WorldNpc(int Id, int DefinitionId, int X, int Y, int Direction)
{
    public static IReadOnlyList<WorldNpc> ParseAppearance(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 2 || packet[0] != 13 || packet[1] != 220)
            throw new InvalidDataException("Unexpected NPC appearance packet.");
        var result = new List<WorldNpc>();
        var rest = packet[2..];
        while (!rest.IsEmpty)
        {
            var end = rest.IndexOf((byte)255);
            if (end != 7) throw new InvalidDataException("Unsupported NPC appearance layout.");
            var id = EoWire.Number(rest[..2]);
            var definition = EoWire.Number(rest.Slice(2,2));
            var x = EoWire.Number(rest.Slice(4,1));
            var y = EoWire.Number(rest.Slice(5,1));
            var direction = EoWire.Number(rest.Slice(6,1)) % 10;
            if (x > 249 || y > 249 || direction > 3) throw new InvalidDataException("Invalid NPC position.");
            result.Add(new(id,definition,x,y,direction));
            rest = rest[(end+1)..];
            if (result.Count > 1024) throw new InvalidDataException("Too many NPCs.");
        }
        return result;
    }
}
