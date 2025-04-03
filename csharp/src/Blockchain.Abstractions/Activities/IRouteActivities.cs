using Temporalio.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;

public interface IRouteActivities
{
    [Activity]
    Task<List<NetworkModel>> GetActiveSolverRouteSourceNetworksAsync();

    [Activity]
    Task<List<RouteModel>> GetAllRoutesAsync();

    [Activity]
    Task UpdateRoutesStatusAsync(int[] routeIds, RouteStatus status);
}