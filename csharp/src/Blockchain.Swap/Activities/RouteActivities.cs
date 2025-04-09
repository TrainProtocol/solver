using Temporalio.Activities;
using Train.Solver.API.Models;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;

namespace Train.Solver.Blockchain.Swap.Activities;

public class RouteActivities(IRouteRepository routeRepository) : IRouteActivities
{
    [Activity]
    public async Task<List<RouteDto>> GetAllRoutesAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active, RouteStatus.Inactive]);

        return routes
            .Select(r => r.ToDto())
            .ToList();
    }

    [Activity]
    public async Task<List<NetworkModel>> GetActiveSolverRouteSourceNetworksAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);

        return routes
            .Where(x => x.Status == RouteStatus.Active)
            .Select(x => new NetworkModel
            {
                Name = x.SourceToken.Network.Name,
                Type = x.SourceToken.Network.Type
            })
            .Distinct()
            .ToList();
    }

    [Activity]
    public virtual async Task UpdateRoutesStatusAsync(
        int[] routeIds,
        RouteStatus status)
    {
        await routeRepository.UpdateRoutesStatusAsync(routeIds, status);
    }
}
