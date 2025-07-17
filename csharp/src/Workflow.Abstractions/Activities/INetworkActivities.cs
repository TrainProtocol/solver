using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface INetworkActivities
{
    [Activity]
    Task<DetailedNetworkDto> GetNetworkAsync(string networkName);
}
