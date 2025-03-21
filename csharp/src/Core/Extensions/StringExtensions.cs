using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Train.Solver.Core.Extensions;

public static class StringExtensions
{
    public static byte[] ToBytes32(this string hexString)
    {
        var bytes = new byte[32];

        var addressBytes = hexString.HexToByteArray();

        Array.Copy(
            sourceArray: addressBytes,
            sourceIndex: 0,
            destinationArray: bytes,
            destinationIndex: bytes.Length - addressBytes.Length,
            length: addressBytes.Length);

        return bytes;
    }

    public static string ConcatHexes(this string firstHex, string secondHex)
    {
        if (secondHex == "0x0")
        {
            return firstHex;
        }

        var low = firstHex.HexToBigInteger(isHexLittleEndian: false);
        var high = secondHex.HexToBigInteger(isHexLittleEndian: false);

        return ((high << 128) + low).ToString("x").EnsureHexPrefix();
    }

    public static string HexStringToAscii(this string hexString)
    {
        StringBuilder ascii = new StringBuilder();

        for (int i = 0; i < hexString.Length; i += 2)
        {
            // Convert each 2-character hex string to a byte
            string hexChar = hexString.Substring(i, 2);
            byte byteValue = Convert.ToByte(hexChar, 16);

            // Convert the byte to an ASCII character and append to the string
            ascii.Append((char)byteValue);
        }

        return ascii.ToString();
    }

    public static string AddAddressPadding(this string address)
    {
        return AddHexPrefix(address.RemoveHexPrefix().PadLeft(64, '0'));
    }

    public static string AddHexPrefix(this string hex)
    {
        return $"0x{hex.RemoveHexPrefix()}";
    }

    public static string EnsureEvenLengthHex(this string hex)
       => hex.Length % 2 != 0 ? $"0{hex}" : hex;
}
