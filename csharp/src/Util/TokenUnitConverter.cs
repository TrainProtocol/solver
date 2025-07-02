using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Util;

public static class TokenUnitConverter
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