using Temporalio.Activities;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface IUtilityActivities
{
    [Activity]
    IEnumerable<BlockRangeModel> GenerateBlockRanges(ulong start, ulong end, uint chunkSize);
}