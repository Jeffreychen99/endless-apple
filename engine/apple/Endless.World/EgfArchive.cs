using System.Buffers.Binary;

namespace Endless.Apple.World;

/// <summary>Reads bitmap resources directly from an EGF PE file; no Windows runtime.</summary>
public sealed class EgfArchive
{
    private readonly byte[] data;
    private readonly int resourceBase;
    private readonly List<(int Rva, int Size, int Offset)> sections = [];
    private readonly Dictionary<int, Raster> cache = [];
    private readonly Dictionary<int, bool> visible = [];
    public EgfArchive(string path)
    {
        data = File.ReadAllBytes(path);
        if (data.Length < 64 || data[0] != 'M' || data[1] != 'Z') throw new InvalidDataException("Invalid EGF file.");
        var pe = I32(60);
        if (I32(pe) != 0x4550) throw new InvalidDataException("Invalid EGF PE header.");
        var count = U16(pe+6); var optionalSize = U16(pe+20); var optional = pe+24;
        if (U16(optional) != 0x10b || count > 32) throw new InvalidDataException("Unsupported EGF PE layout.");
        for (var i = 0; i < count; i++)
        {
            var p = optional + optionalSize + i*40;
            sections.Add((I32(p+12), I32(p+16), I32(p+20)));
        }
        resourceBase = Offset(I32(optional+112));
    }
    private ReadOnlySpan<byte> Slice(int p, int n)
    {
        if (p < 0 || n < 0 || p > data.Length-n) throw new InvalidDataException("Truncated EGF resource.");
        return data.AsSpan(p,n);
    }
    private int I32(int p) => BinaryPrimitives.ReadInt32LittleEndian(Slice(p,4));
    private int U16(int p) => BinaryPrimitives.ReadUInt16LittleEndian(Slice(p,2));
    private int Offset(int rva)
    {
        foreach (var s in sections) if (rva >= s.Rva && (long)rva < (long)s.Rva+s.Size) return checked(s.Offset+rva-s.Rva);
        throw new InvalidDataException("EGF resource address is outside its sections.");
    }
    private int? FindEntry(int directory, int? id)
    {
        var p = checked(resourceBase+directory); var count = U16(p+12)+U16(p+14);
        for (var i = 0; i < count; i++)
        {
            var key = I32(p+16+i*8);
            if (id == null || key == id) return I32(p+20+i*8);
        }
        return null;
    }
    private int Entry(int directory, int? id) => FindEntry(directory,id)
        ?? throw new InvalidDataException("Required artwork is missing from the graphics archive.");
    public Raster? OptionalBitmap(int graphic)
    {
        if (cache.TryGetValue(graphic,out var found)) return found;
        var type = Entry(0,2);
        return FindEntry(type & int.MaxValue,checked(graphic+100)) == null ? null : Bitmap(graphic);
    }
    public bool HasVisibleBitmap(int graphic)
    {
        if (visible.TryGetValue(graphic,out var found)) return found;
        var bitmap = OptionalBitmap(graphic);
        var value = false;
        if (bitmap is not null)
            for (var i=3; i<bitmap.Rgba.Length; i+=4)
                if (bitmap.Rgba[i]!=0) { value=true; break; }
        visible.Add(graphic,value);
        return value;
    }
    public Raster Bitmap(int graphic)
    {
        if (cache.TryGetValue(graphic,out var found)) return found;
        var type = Entry(0,2);
        var name = Entry(type & int.MaxValue,checked(graphic+100));
        var language = Entry(name & int.MaxValue,null);
        if (type >= 0 || name >= 0 || language < 0) throw new InvalidDataException("Unexpected bitmap resource tree.");
        var entry = checked(resourceBase+language);
        var dib = Slice(Offset(I32(entry)),I32(entry+4));
        var raster = Raster.FromDib(dib);
        cache.Add(graphic,raster); return raster;
    }
}

public sealed record Raster(int Width, int Height, byte[] Rgba)
{
    public static Raster FromDib(ReadOnlySpan<byte> dib)
    {
        if (dib.Length < 40) throw new InvalidDataException("Truncated bitmap.");
        var header = BinaryPrimitives.ReadInt32LittleEndian(dib);
        var width = BinaryPrimitives.ReadInt32LittleEndian(dib[4..]);
        var signedHeight = BinaryPrimitives.ReadInt32LittleEndian(dib[8..]);
        var bits = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
        var compression = BinaryPrimitives.ReadInt32LittleEndian(dib[16..]);
        if (header < 40 || width <= 0 || width > 4096 || signedHeight == 0 || signedHeight is < -4096 or > 4096
            || (compression != 0 && !(compression == 3 && bits == 16)) || bits is not (8 or 16 or 24 or 32)) throw new InvalidDataException("Unsupported bitmap format.");
        var height = Math.Abs(signedHeight);
        var colors = bits == 8 ? BinaryPrimitives.ReadInt32LittleEndian(dib[32..]) : 0;
        if (bits == 8 && colors == 0) colors = 256;
        if (colors < 0 || colors > 256) throw new InvalidDataException("Invalid palette.");
        uint red = 0x7c00, green = 0x03e0, blue = 0x001f;
        var masksSize = compression == 3 && header == 40 ? 12 : 0;
        if (compression == 3)
        {
            if (dib.Length < 52) throw new InvalidDataException("Truncated bitmap masks.");
            red = BinaryPrimitives.ReadUInt32LittleEndian(dib[40..]);
            green = BinaryPrimitives.ReadUInt32LittleEndian(dib[44..]);
            blue = BinaryPrimitives.ReadUInt32LittleEndian(dib[48..]);
            if (!((red == 0xf800 && green == 0x07e0) || (red == 0x7c00 && green == 0x03e0)) || blue != 0x001f)
                throw new InvalidDataException("Unsupported bitmap color masks.");
        }
        var offset = checked(header+colors*4+masksSize); var stride = checked((width*bits+31)/32*4);
        if ((long)offset+(long)stride*height > dib.Length) throw new InvalidDataException("Truncated bitmap pixels.");
        var pixels = new byte[checked(width*height*4)];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var p = offset + (signedHeight > 0 ? height-1-y : y)*stride+x*(bits/8);
            if (bits == 8)
            {
                var index = dib[p]; if (index >= colors) throw new InvalidDataException("Invalid palette index.");
                p = header+index*4;
            }
            var q = (y*width+x)*4;
            if (bits == 16)
            {
                var value = BinaryPrimitives.ReadUInt16LittleEndian(dib[p..]);
                byte Channel(uint mask)
                {
                    var shift = System.Numerics.BitOperations.TrailingZeroCount(mask);
                    return (byte)(((value & mask) >> shift)*255/(mask >> shift));
                }
                pixels[q] = Channel(red); pixels[q+1] = Channel(green); pixels[q+2] = Channel(blue);
                pixels[q+3] = (byte)(value == 0 ? 0 : 255);
                continue;
            }
            pixels[q] = dib[p+2]; pixels[q+1] = dib[p+1]; pixels[q+2] = dib[p];
            pixels[q+3] = (byte)((dib[p] | dib[p+1] | dib[p+2]) == 0 ? 0 : 255);
        }
        return new(width,height,pixels);
    }
}
