using Endless.Apple.Protocol;
using System.Text;

namespace Endless.Apple.World;

public sealed record MapTile(int X, int Y, int Graphic, int Height = 0);
public sealed record WorldMap(string Name, int Width, int Height, int FillTile, IReadOnlyList<MapTile>[] Layers)
{
    public IReadOnlySet<(int X,int Y)> RestrictedTiles { get; init; } = new HashSet<(int,int)>();
    // Until special tiles and warps are implemented, permit only ordinary floor.
    public bool CanWalk(int x,int y) => x >= 0 && y >= 0 && x < Width && y < Height && !RestrictedTiles.Contains((x,y));
    public static WorldMap Load(byte[] data)
    {
        if (data.Length < 135 || !data.AsSpan(0, 3).SequenceEqual("EMF"u8))
            throw new InvalidDataException("Invalid current map header.");
        if (data[7] != 1 || data[8] != 5 || data[132] is not (1 or 2 or 3))
            throw new InvalidDataException("Unsupported map version or compression.");
        var width = EoWire.Number(data.AsSpan(11,1)) + 1;
        var height = EoWire.Number(data.AsSpan(12,1)) + 1;
        if (width > 250 || height > 250) throw new InvalidDataException("Map dimensions exceed client limits.");
        var nameBytes = data.AsSpan(14,48); var end = nameBytes.IndexOf((byte)255);
        var name = Encoding.Latin1.GetString(end >= 0 ? nameBytes[..end] : nameBytes);
        var fill = EoWire.Number(data.AsSpan(62,2));
        var reader = new MapReader(data, 134);
        var restricted = new HashSet<(int,int)>();
        void RestrictedRows(int size)
        {
            var rows = reader.Number();
            if (rows > height) throw new InvalidDataException("Invalid special-tile rows.");
            for (var r=0;r<rows;r++)
            {
                var y=reader.Number(); var count=reader.Number();
                if (y>=height || count>width) throw new InvalidDataException("Invalid special-tile row.");
                for (var i=0;i<count;i++)
                {
                    var x=reader.Number(); reader.Skip(size-1);
                    if (x>=width) throw new InvalidDataException("Invalid special tile.");
                    restricted.Add((x,y));
                }
            }
        }
        RestrictedRows(2); reader.Array(18); reader.Array(8); RestrictedRows(18);
        reader.Array(13); reader.Rows(4); reader.Array(0); reader.Array(17);
        reader.Array(4); reader.Array(13); reader.Array(4); reader.Array(4);
        var tail = data.AsSpan(reader.Position);
        var graphics = data[132] switch { 2 => Expand(tail), 3 => ExpandDictionary(tail), _ => tail.ToArray() };
        reader = new MapReader(graphics, 1);
        reader.Rows(3); reader.Rows(3); reader.Skip(1);
        var layers = new IReadOnlyList<MapTile>[14];
        for (var layer = 0; layer < layers.Length; layer++)
        {
            var tiles = new List<MapTile>();
            var seen = new HashSet<(int,int)>();
            var rows = reader.Number();
            if (rows > height) throw new InvalidDataException("Invalid graphic row count.");
            for (var r = 0; r < rows; r++)
            {
                var y = reader.Number(); var count = reader.Number();
                if (y >= height || count > width) throw new InvalidDataException("Invalid graphic row.");
                for (var i = 0; i < count; i++)
                {
                    var x = reader.Number(); var graphic = reader.Number(2);
                    // Raised tiles carry an additional elevation byte (official 0x4639d7).
                    var elevation = layer == 3 ? reader.Number() : 0;
                    if (x >= width || !seen.Add((x,y))) throw new InvalidDataException("Invalid or duplicate map tile.");
                    tiles.Add(new(x,y,graphic,elevation));
                }
            }
            layers[layer] = tiles.AsReadOnly();
        }
        return new(name,width,height,fill,layers) {RestrictedTiles=restricted};
    }

    // Official 0x721814 initializes literal codes 0..255 and reads big-endian shorts.
    public static byte[] ExpandDictionary(ReadOnlySpan<byte> source)
    {
        if (source.Length < 2 || source.Length%2 != 0) throw new InvalidDataException("Truncated dictionary-compressed map.");
        var dictionary = Enumerable.Range(0,256).Select(i => new byte[] {(byte)i}).ToList();
        var first = (source[0]<<8)|source[1];
        if (first > 255) throw new InvalidDataException("Invalid initial map dictionary code.");
        var previous = dictionary[first];
        var output = new List<byte>(previous);
        for (var i=2;i<source.Length;i+=2)
        {
            var code = (source[i]<<8)|source[i+1];
            byte[] entry;
            if (code < dictionary.Count) entry = dictionary[code];
            else if (code == dictionary.Count) entry = [..previous,previous[0]];
            else throw new InvalidDataException("Invalid map dictionary code.");
            if (output.Count > 16_000_000-entry.Length) throw new InvalidDataException("Expanded map is too large.");
            output.AddRange(entry);
            if (dictionary.Count < 65536) dictionary.Add([..previous,entry[0]]);
            previous = entry;
        }
        return output.ToArray();
    }

    public static byte[] Expand(ReadOnlySpan<byte> source)
    {
        // Official decoder 0x721600: FF, raw backward distance, raw copy length.
        var output = new List<byte>();
        for (var i = 0; i < source.Length; i++)
        {
            if (output.Count > 16_000_000 - 255) throw new InvalidDataException("Expanded map is too large.");
            if (source[i] != 255) { output.Add(source[i]); continue; }
            if (i + 2 >= source.Length) throw new InvalidDataException("Truncated map back-reference.");
            var distance = source[++i]; var count = source[++i];
            if (distance == 0 || distance > output.Count || count == 0)
                throw new InvalidDataException("Invalid map back-reference.");
            for (var n = 0; n < count; n++) output.Add(output[output.Count - distance]);
        }
        return output.ToArray();
    }

    private sealed class MapReader(byte[] data, int position)
    {
        public int Position { get; private set; } = position;
        public int Number(int width = 1)
        {
            var start = Position; Skip(width);
            return EoWire.Number(data.AsSpan(start,width));
        }
        public void Skip(int count)
        {
            if (count < 0 || count > data.Length - Position) throw new InvalidDataException("Truncated map data.");
            Position += count;
        }
        public void Array(int size) { var count = Number(); Skip(count * size); }
        public void Rows(int size)
        {
            var count = Number();
            for (var i = 0; i < count; i++) { Number(); var cells = Number(); Skip(cells * size); }
        }
    }
}
