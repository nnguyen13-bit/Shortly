namespace Shortly.Infrastructure.CodeGeneration;

public static class Base62
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Encode(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");

        if (value == 0)
            return "0";

        var chars = new char[13]; // max chars for long
        var index = chars.Length;

        while (value > 0)
        {
            chars[--index] = Alphabet[(int)(value % 62)];
            value /= 62;
        }

        return new string(chars, index, chars.Length - index);
    }

    public static long Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            throw new ArgumentException("Encoded string cannot be empty.", nameof(encoded));

        long result = 0;
        foreach (var c in encoded)
        {
            var digit = Alphabet.IndexOf(c);
            if (digit < 0)
                throw new ArgumentException($"Invalid Base62 character: '{c}'", nameof(encoded));

            result = result * 62 + digit;
        }

        return result;
    }
}
