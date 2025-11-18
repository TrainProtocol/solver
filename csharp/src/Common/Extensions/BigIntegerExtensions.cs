using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Train.Solver.Common.Helpers;

namespace Train.Solver.Common.Extensions;

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

    public static BigInteger Average(this IEnumerable<BigInteger> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var list = source as ICollection<BigInteger> ?? source.ToList();
        if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements");

        BigInteger sum = BigInteger.Zero;
        foreach (var val in list)
            sum += val;

        return sum / list.Count;
    }

    public static decimal ToUsd(this BigInteger amount, decimal priceUsd, int decimals)
    {
        if (decimals < 0 || decimals > 77)
            throw new ArgumentOutOfRangeException(nameof(decimals), "Decimals must be between 0 and 77 for decimal precision.");

        // Convert divisor to decimal manually
        var divisor = BigInteger.Pow(10, decimals);
        decimal normalizedAmount = (decimal)(amount / divisor) + (decimal)(amount % divisor) / (decimal)divisor;

        return (normalizedAmount * priceUsd).Truncate(2);
    }

    public static BigInteger PercentOf(this BigInteger value, decimal percent)
    {
        if (percent < 0) throw new ArgumentOutOfRangeException(nameof(percent), "Percentage must be non-negative.");

        // Convert 10.5% to 1050 with a scale of 10000 (4 decimal places)
        const int scale = 10000;
        var scaledPercent = (BigInteger)(percent * scale);

        return value * scaledPercent / scale / 100;
    }


    public static BigInteger ConvertTokenAmount(
        this BigInteger amount,
        int fromDecimals,
        int toDecimals,
        decimal rate)
    {
        var amountInDecimal = TokenUnitHelper.FromBaseUnits(amount, fromDecimals);

        amountInDecimal /= rate;

        return TokenUnitHelper.ToBaseUnits(amountInDecimal, toDecimals);
    }
}
