using System.Numerics;
using System.Text.RegularExpressions;

namespace Train.Solver.Blockchain.Solana.Extensions;

public static class TransactionLogExtension
{
    public static BigInteger ExtractTotalComputeUnitsUsed(List<string> logs)
    {
        int totalUnitsUsed = 0;

        foreach (var log in logs)
        {
            if (log.Contains("Program") && log.Contains("consumed"))
            {
                var match = Regex.Match(log, @"Program \S+ consumed (\d+) of \d+ compute units");

                if (match.Success && int.TryParse(match.Groups[1].Value, out int unitsUsed))
                {
                    totalUnitsUsed += unitsUsed;
                }

            }
        }

        return totalUnitsUsed;
    }
}
