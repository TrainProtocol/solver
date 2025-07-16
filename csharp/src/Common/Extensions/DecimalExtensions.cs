namespace Train.Solver.Common.Extensions;

public static class DecimalExtensions
{
    public static decimal Truncate(this decimal number, int decimals) =>
        Math.Truncate(number * (decimal)Math.Pow(10, decimals)) / (decimal)Math.Pow(10, decimals);
}
