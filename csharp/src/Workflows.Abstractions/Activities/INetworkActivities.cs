using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflows.Abstractions.Activities;

public interface INetworkActivities
{
    [Activity]
    Task<DetailedNetworkDto> GetNetworkAsync(string networkName);
}
