using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Common.Activities;

public class RouteActivities(IRouteRepository routeRepository) : IRouteActivities
{
    [Activity]
    public async Task<List<RouteModel>> GetAllRoutesAsync()
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active, RouteStatus.Inactive]);

        return routes
            .Select(r => new RouteModel
            {
                Id = r.Id,
                Status = r.Status,
                MaxAmountInSource = r.MaxAmountInSource,

                Source = new TokenModel
                {
                    Id = r.SourceToken.Id,
                    NetworkName = r.SourceToken.Network.Name,
                    Asset = r.SourceToken.Asset,
                    Precision = r.SourceToken.Precision,
                    IsTestnet = r.SourceToken.Network.IsTestnet,
                    NetworkId = r.DestinationToken.NetworkId,
                    NetworkType = r.SourceToken.Network.Type
                },

                Destionation = new TokenModel
                {
                    Id = r.DestinationToken.Id,
                    NetworkName = r.DestinationToken.Network.Name,
                    Asset = r.DestinationToken.Asset,
                    Precision = r.DestinationToken.Precision,
                    IsTestnet = r.DestinationToken.Network.IsTestnet,
                    NetworkId = r.DestinationToken.NetworkId,
                    NetworkType = r.DestinationToken.Network.Type
                }
            })
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
