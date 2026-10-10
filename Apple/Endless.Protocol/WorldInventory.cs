namespace Endless.Apple.Protocol;

public sealed record InventoryEntry(int ItemId,int Amount);
public sealed record WorldInventory(int Weight,int Capacity,IReadOnlyList<InventoryEntry> Entries)
{
    public static WorldInventory Parse(ReadOnlySpan<byte> packet)
    {
        // Official 241/226 -> 0x5a085c: two weight shorts followed by
        // 14-byte records: ten-byte item descriptor and four-byte amount.
        if(packet.Length<6 || packet[0]!=241 || packet[1]!=226)
            throw new InvalidDataException("Truncated inventory snapshot.");
        var body=packet[2..];
        if((body.Length-4)%14!=0 || (body.Length-4)/14>1024)
            throw new InvalidDataException("Unsupported inventory snapshot length.");
        var weight=EoWire.Number(body[..2]);
        var capacity=EoWire.Number(body.Slice(2,2));
        var entries=new List<InventoryEntry>();
        for(var offset=4;offset<body.Length;offset+=14)
        {
            // Descriptor ID is the standard short decoder at 0x66bcdc.
            var id=EoWire.Number(body.Slice(offset,2));
            var amount=EoWire.Number(body.Slice(offset+10,4));
            if(id<=0 || amount<=0)throw new InvalidDataException("Invalid inventory entry.");
            entries.Add(new(id,amount));
        }
        return new(weight,capacity,entries);
    }
}
