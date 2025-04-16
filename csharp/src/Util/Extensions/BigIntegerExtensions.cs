using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;

namespace Train.Solver.Util.Extensions;

public static class BigIntegerExtensions
{
    public static BigInteger PercentageIncrease(this BigInteger num, int percentage)
        => BigInteger.Divide(
            dividend: BigInteger.Multiply(num, 100 + percentage),
            divisor: 100);

    public static decimal PercentageIncrease(this decimal num, int percentage)
        => decimal.Divide(
               d1: decimal.Multiply(num, 100 + percentage),
               d2: 100);

    public static BigInteger CompoundInterestRate(this BigInteger num, double rate, int periods)
    {
        var increasePercentage = (int)(Math.Pow(1 + rate, periods) * 100) - 100;
        return num.PercentageIncrease(increasePercentage);
    }

    public static bool TryParse(
        string firstHex,
        string secondHex,
        out BigInteger result)
    {
        string hexNumber = firstHex.ConcatHexes(secondHex);
        
        try
        {
            result = hexNumber.HexToBigInteger(isHexLittleEndian: false);
        }
        catch (Exception)
        {
            result = BigInteger.Zero;
            return false;
        }

        return true;
    }

    public static bool InRange(this HexBigInteger addressInt, BigInteger lowerBound, BigInteger upperBound)
    {
        return addressInt.Value.CompareTo(lowerBound) > 0 &&
               addressInt.Value.CompareTo(upperBound) < 0;
    }
}
