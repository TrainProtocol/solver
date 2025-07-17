using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public interface INetworkActivities
{
    [Activity]
    Task<DetailedNetworkDto> GetNetworkAsync(string networkName);
}
