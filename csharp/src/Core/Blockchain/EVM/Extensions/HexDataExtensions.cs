namespace Train.Solver.Core.Blockchain.EVM.Extensions;

public static class HexDataExtensions
{
    public static string EnsureEvenLengthHex(this string hex)
        => hex.Length % 2 != 0 ? $"0{hex}" : hex;
}
