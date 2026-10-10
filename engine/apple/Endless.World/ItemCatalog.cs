using Endless.Apple.Protocol;

namespace Endless.Apple.World;

public sealed record ItemAppearance(int Graphic, int Type, int Subtype)
{
    public string Name {get;init;} = "";
    public int InventoryGraphic {get;init;}
    public int InventorySize {get;init;}
    public (int Width,int Height) Footprint => InventorySize is >=0 and <=7
        ? (InventorySize/4+1,InventorySize%4+1) : (1,1);
}

public sealed class ItemCatalog
{
    private readonly List<ItemAppearance> items = [];
    public int Count => items.Count;
    public ItemAppearance? Find(int id) => id > 0 && id <= items.Count ? items[id-1] : null;
    public static ItemCatalog Load(string root)
    {
        var result = new ItemCatalog();
        foreach (var path in Directory.GetFiles(Path.Combine(root,"data","pub"),"dat_items*.epf").Order(StringComparer.Ordinal))
            result.Append(File.ReadAllBytes(path));
        if (result.Count == 0) throw new InvalidDataException("Item tables are missing.");
        return result;
    }
    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.Length < 13 || !data[..3].SequenceEqual("EIF"u8) || data[10] != 5 || data[11] != 58)
            throw new InvalidDataException("Unsupported item table version.");
        var parsed = new List<ItemAppearance>();
        var offset = 13;
        while (offset < data.Length)
        {
            var length = EoWire.Number(data.Slice(offset++,1));
            if (length > 50 || offset+length+4 > data.Length) throw new InvalidDataException("Truncated item name.");
            var name=System.Text.Encoding.Latin1.GetString(data.Slice(offset,length));
            var reserved = data.Slice(offset,length).SequenceEqual("r"u8);
            offset += length;
            var size = reserved ? 4 : 78;
            if (offset+size > data.Length) throw new InvalidDataException("Truncated item record.");
            var type = EoWire.Number(data.Slice(offset+3,1));
            var graphic = reserved || type is < 10 or > 28 ? 0 : EoWire.Number(data.Slice(offset+47,3));
            parsed.Add(new(graphic,type,reserved ? 0 : EoWire.Number(data.Slice(offset+4,1))) {Name=reserved ? "Unknown item" : name,InventoryGraphic=reserved?0:EoWire.Number(data.Slice(offset+1,2)),InventorySize=reserved?0:EoWire.Number(data.Slice(offset+74,1))});
            offset += size;
            if (items.Count+parsed.Count > 64008) throw new InvalidDataException("Too many items.");
        }
        items.AddRange(parsed);
    }
}
