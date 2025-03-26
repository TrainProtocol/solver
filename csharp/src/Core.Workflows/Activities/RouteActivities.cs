using Temporalio.Activities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Abstractions.Repositories;

namespace Train.Solver.Core.Workflows.Activities;

public class RouteActivities(IRouteRepository routeRepository)
{
    [Activity]
    public async Task<List<RouteModel>> GetAllRoutesAsync()
    {
        var routes = await routeRepository.GetAllAsync();

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
        var routes = await routeRepository.GetAllAsync();

        return routes
            .Where(x => x.Status == Abstractions.Entities.RouteStatus.Active)
            .Select(x => new NetworkModel
            {
                Name = x.SourceToken.Network.Name,
                Type = x.SourceToken.Network.Type
            })
            .Distinct()
            .ToList();
    }

    [Activity]
    public Dictionary<int, string> GetManagedAddressesByNetworkAsync()
    {
        return null;
        //dbContext.Networks
        //     .Include(x => x.ManagedAccounts)
        //     .ToDictionary(x => x.Id, x => x.ManagedAccounts.First().Address);
    }

    [Activity]
    public virtual async Task UpdateDestinationRouteStatusAsync(
      IEnumerable<int> routeIds,
      decimal balance)
    {
        //var routes = await dbContext.Routes
        //    .Where(x => routeIds.Any(y => y == x.Id))
        //    .ToListAsync();

        //foreach (var route in routes)
        //{
        //    if (balance < route.MaxAmountInSource && route.Status == RouteStatus.Active)
        //    {
        //        route.Status = RouteStatus.Inactive;
        //    }
        //    else if (balance >= route.MaxAmountInSource && route.Status == RouteStatus.Inactive)
        //    {
        //        route.Status = RouteStatus.Active;
        //    }
        //}

        //await dbContext.SaveChangesAsync();
    }
}
