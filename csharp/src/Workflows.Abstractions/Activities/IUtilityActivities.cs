using Temporalio.Activities;
using Train.Solver.Workflows.Abstractions.Models;

namespace Train.Solver.Workflows.Abstractions.Activities;

public interface IUtilityActivities
{
    [Activity]
    IEnumerable<BlockRangeModel> GenerateBlockRanges(ulong start, ulong end, uint chunkSize);
}