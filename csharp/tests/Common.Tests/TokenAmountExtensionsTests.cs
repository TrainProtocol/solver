using Shouldly;
using System.Numerics;
using Train.Solver.Common.Extensions;

namespace Common.Tests;

public class TokenAmountExtensionsTests
{
    [Theory]
    [InlineData("1000000000", 18, 18, 1, "1000000000")] 
    [InlineData("1000000000", 9, 18, 1, "1000000000000000000")]
    [InlineData("1000000000000000000", 18, 9 , 1, "1000000000")]
    [InlineData("500000000", 8, 6, 2, "10000000")]
    public void ConvertTokenAmount_ParameterizedCases(
        string amountStr, int fromDecimals, int toDecimals,
        decimal rate, string expectedStr)
    {
        var amount = BigInteger.Parse(amountStr);
        var expected = BigInteger.Parse(expectedStr);

        var result = amount.ConvertTokenAmount(fromDecimals, toDecimals, rate);
        result.ShouldBe(expected);
    }
}