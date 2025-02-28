using System.Security.Cryptography;

namespace Train.Solver.Core.Blockchain.Solana.Helpers;

public static class FieldEncoder
{
    public class Field
    {
        public int Span { get; set; }
        public string Property { get; set; }
        public Action<object, byte[], int> EncoderFunc { get; set; }

        public int Encode(object src, byte[] buffer, int offset = 0)
        {
            if (EncoderFunc == null)
            {
                throw new InvalidOperationException("No encoding method provided.");
            }
            EncoderFunc(src, buffer, offset);
            return Span;
        }
    }

    public static byte[] Encode(List<Field> fields, Dictionary<string, object> src, byte[] descriminator)
    {
        var buffer = new byte[1000];
        var offset = 8;

        foreach (var field in fields)
        {
            if (src.TryGetValue(field.Property, out var value))
            {
                offset += field.Encode(value, buffer, offset);
            }
        }

        Array.Copy(descriminator, buffer, descriminator.Length);

        return buffer.Take(offset).ToArray();
    }

    public static byte[] Sighash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return hash.Take(8).ToArray();
    }

    public static void EncodeByteArrayWithLength(byte[] byteArray, byte[] buffer, ref int offset)
    {
        buffer[offset] = (byte)byteArray.Length;
        offset += 4;
        Array.Copy(byteArray, 0, buffer, offset, byteArray.Length);
        offset += byteArray.Length;
    }

    public static void EncodeByteArray(byte[] byteArray, byte[] buffer, ref int offset)
    {
        Array.Copy(byteArray, 0, buffer, offset, byteArray.Length);
        offset += byteArray.Length;
    }

    public static byte[] StringToByteArray(this string hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}
