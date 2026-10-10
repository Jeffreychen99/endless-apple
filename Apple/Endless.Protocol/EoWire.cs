using System.Net.Sockets;

namespace Endless.Apple.Protocol;

/// <summary>EO base-253 numbers and length-delimited TCP frames. No credentials or gameplay writes.</summary>
public static class EoWire
{
    public static byte[] Number(int value, int width)
    {
        if (width is < 1 or > 4 || value < 0 || value >= Math.Pow(253, width))
            throw new ArgumentOutOfRangeException(nameof(value));
        var result = Enumerable.Repeat((byte)254, width).ToArray();
        for (var i = 0; i == 0 || value > 0; i++, value /= 253)
            result[i] = (byte)(value % 253 + 1);
        return result;
    }

    public static int Number(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > 4) throw new InvalidDataException("Invalid number width.");
        long result = 0, place = 1;
        var padding = false;
        foreach (var b in bytes)
        {
            if (b is 0 or 255 || (padding && b != 254)) throw new InvalidDataException("Invalid EO number.");
            if (b == 254) padding = true;
            else result += (b - 1) * place;
            place *= 253;
        }
        if (result > int.MaxValue) throw new InvalidDataException("EO number exceeds the supported range.");
        return (int)result;
    }

    public static byte[] Frame(byte[] body)
    {
        if (body.Length < 2) throw new InvalidDataException("Missing packet identifiers.");
        return [.. Number(body.Length, 2), .. body];
    }

    public static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken token)
    {
        var header = new byte[2];
        await stream.ReadExactlyAsync(header, token);
        var length = Number(header);
        if (length < 2) throw new InvalidDataException("Invalid packet length.");
        var body = new byte[length];
        await stream.ReadExactlyAsync(body, token);
        return body;
    }
}
