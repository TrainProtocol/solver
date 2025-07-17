using Temporalio.Activities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Common.Enums;
using Train.Solver.Workflows.Abstractions.Activities;

namespace Train.Solver.Workflows.Swap.Activities;

public class RouteActivities(IRouteRepository routeRepository) : IRouteActivities
{
    [Activity]
    public async Task<List<RouteDetailedDto>> GetAllRoutesAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active, RouteStatus.Inactive]);

        return routes
            .Select(r => r.ToDetailedDto())
            .ToList();
    }

    [Activity]
    public async Task<List<NetworkDto>> GetActiveSolverRouteSourceNetworksAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);

        return routes
            .Select(x => x.SourceToken.Network.ToDto())
            .DistinctBy(x => x.Name)
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
