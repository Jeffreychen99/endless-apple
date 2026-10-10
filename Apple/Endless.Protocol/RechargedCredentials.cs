namespace Endless.Apple.Protocol;

/// <summary>Current credential encoding recovered from 0x5050b8 / 0x4f5e78.
/// Not a secure transport or a password hash. Used by the explicit native sign-in test.</summary>
public static class RechargedCredentials
{
    public static byte[] Encode(ReadOnlySpan<char> text, byte parameter)
    {
        if (text.Length is < 1 or > 16)
            throw new ArgumentException("Credentials must contain 1–16 ASCII characters.");
        foreach (var character in text)
            if (character is < ' ' or > '~')
                throw new ArgumentException("Only printable ASCII credentials are supported.");
        var output = new List<byte>(text.Length * 3);
        for (var i = 1; i <= text.Length; i++)
        {
            var value = (text[i - 1] + parameter * i - (i % 2 == 0 ? 12 : 0)) & 255;
            if (i % 3 == 0)
            {
                value += 60;
                output.Add((byte)('A' + value / 576));
                output.Add((byte)('A' + value % 576 / 24));
            }
            else output.Add((byte)('A' + value / 24));
            output.Add((byte)('A' + value % 24));
        }
        return output.ToArray();
    }
}
