using System;
using System.Collections;
using System.Linq;
public static class BinaryConverter
{
    public static BitArray ToBinary(this int numeral)
    {
        return new BitArray(new[] { numeral });
    }

    public static int ToNumeral(this BitArray binary)
    {
        if (binary == null)
            throw new ArgumentNullException("binary");
        if (binary.Length > 32)
            throw new ArgumentException("must be at most 32 bits long");

        var result = new int[1];
        binary.CopyTo(result, 0);
        return result[0];
    }

    public static short ToShort(this BitArray binary)
    {
        if (binary == null)
            throw new ArgumentNullException("binary");
        if (binary.Length > 16)
            throw new ArgumentException("must be at most 32 bits long");

        var result = new short[1];
        binary.CopyTo(result, 0);
        return result[0];
    }

    public static bool isEquivalent(this BitArray a, BitArray b)
    {
        return (a.ToNumeral() == b.ToNumeral());
    }
}