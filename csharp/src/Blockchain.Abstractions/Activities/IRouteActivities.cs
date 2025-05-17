using Temporalio.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

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