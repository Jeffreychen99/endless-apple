using Endless.Apple.Protocol;

namespace Endless.Apple.World;

public sealed class NpcCatalog
{
    private readonly List<int> graphics = [];
    public int Count => graphics.Count;
    public int Graphic(int id) => id > 0 && id <= Count ? graphics[id-1] : 0;
    public static NpcCatalog Load(string root)
    {
        var result = new NpcCatalog();
        foreach (var path in Directory.GetFiles(Path.Combine(root,"data","pub"),"dat_npcs*.epf").Order(StringComparer.Ordinal))
            result.Append(File.ReadAllBytes(path));
        if (result.Count == 0) throw new InvalidDataException("NPC tables are missing.");
        return result;
    }
    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.Length < 13 || !data[..3].SequenceEqual("ENF"u8) || data[10] != 5 || data[11] != 58)
            throw new InvalidDataException("Unsupported NPC table version.");
        var parsed = new List<int>();
        var offset = 13;
        while (offset < data.Length)
        {
            var length = EoWire.Number(data.Slice(offset++,1));
            if (length > 50 || offset+length > data.Length) throw new InvalidDataException("Truncated NPC name.");
            var reserved = data.Slice(offset,length).SequenceEqual("r"u8);
            offset += length;
            var size = reserved ? 3 : 85;
            if (offset+size > data.Length) throw new InvalidDataException("Truncated NPC record.");
            // Official 0x67b918..0x67b933: flags byte, then graphic short.
            parsed.Add(reserved ? 0 : EoWire.Number(data.Slice(offset+1,2)));
            offset += size;
            if (Count+parsed.Count > 64008) throw new InvalidDataException("Too many NPC definitions.");
        }
        graphics.AddRange(parsed);
    }
}
