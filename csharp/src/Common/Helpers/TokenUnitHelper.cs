using Nethereum.Util;
using System.Numerics;

namespace Train.Solver.Common.Helpers;

public static class TokenUnitHelper
{
    private static readonly UnitConversion _converter = new();

    /// <summary>
    /// Converts a decimal token amount to its BigInteger wei representation.
    /// </summary>
    public static BigInteger ToBaseUnits(decimal amount, int decimals)
    {
        return _converter.ToWei(amount, decimals);
    }

    /// <summary>
    /// Converts a BigInteger wei amount to its decimal token representation.
    /// </summary>
    public static decimal FromBaseUnits(BigInteger amount, int decimals)
    {
        return _converter.FromWei(amount, decimals);
    }
}