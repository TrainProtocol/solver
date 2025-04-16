using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public interface IUtilityActivities
{
    [Activity]
    IEnumerable<BlockRangeModel> GenerateBlockRanges(ulong start, ulong end, uint chunkSize);
}