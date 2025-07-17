using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Common.Enums;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public interface IRouteActivities
{
    [Activity]
    Task<List<NetworkDto>> GetActiveSolverRouteSourceNetworksAsync();

    [Activity]
    Task<List<RouteDetailedDto>> GetAllRoutesAsync();

    [Activity]
    Task UpdateRoutesStatusAsync(int[] routeIds, RouteStatus status);
}