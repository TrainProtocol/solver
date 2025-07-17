using Temporalio.Activities;
using Train.Solver.Workflows.Abstractions.Activities;
using Train.Solver.Workflows.Abstractions.Models;

namespace Train.Solver.Workflows.Swap.Activities;


public class UtilityActivities : IUtilityActivities
{
    [Activity]
    public IEnumerable<BlockRangeModel> GenerateBlockRanges(ulong start, ulong end, uint chunkSize)
    {
        if (chunkSize == 0)
            throw new ArgumentException("Max size must be greater than 0", nameof(chunkSize));

        var result = new List<BlockRangeModel>();

        ulong currentStart = start;

        while (currentStart <= end)
        {
            ulong currentEnd = Math.Min(currentStart + chunkSize - 1, end);
            result.Add(new BlockRangeModel(currentStart, currentEnd));
            currentStart = currentEnd + 1;
        }

        return result;
    }
}
