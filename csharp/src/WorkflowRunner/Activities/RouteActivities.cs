using Microsoft.EntityFrameworkCore;
using Serilog;
using Temporalio.Activities;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Models;

namespace Train.Solver.WorkflowRunner.Activities;

public class RouteActivities(
    SolverDbContext dbContext)
{
    [Activity]
    public async Task<List<RouteModel>> GetAllRoutesAsync()
    {
        return await dbContext.Routes
            .Select(r => new RouteModel
            {
                Id = r.Id,
                Status = r.Status,
                MaxAmountInSource = r.MaxAmountInSource,

                SourceTokenModel = new TokenModel
                {
                    Id = r.SourceToken.Id,
                    NetworkName = r.SourceToken.Network.Name,
                    Asset = r.SourceToken.Asset,
                    Precision = r.SourceToken.Precision,
                    IsTestnet = r.SourceToken.Network.IsTestnet,
                    NetworkId = r.DestinationToken.NetworkId
                },

                DestionationTokenModel = new TokenModel
                {
                    Id = r.DestinationToken.Id,
                    NetworkName = r.DestinationToken.Network.Name,
                    Asset = r.DestinationToken.Asset,
                    Precision = r.DestinationToken.Precision,
                    IsTestnet = r.DestinationToken.Network.IsTestnet,
                    NetworkId = r.DestinationToken.NetworkId
                }
            })
            .ToListAsync();
    }
    [Activity]
    public async Task<List<string>> GetActiveSolverRouteSourceNetworkNamesAsync()
    {
        return await dbContext.Routes
            .Where(x => x.Status == RouteStatus.Active)
            .Include(x => x.SourceToken)
            .ThenInclude(x => x.Network)
            .Select(x => x.SourceToken.Network.Name)
            .Distinct()
            .ToListAsync();
    }

    [Activity]
    public Dictionary<int, string> GetManagedAddressesByNetworkAsync()
    {
        return dbContext.Networks
             .Include(x => x.ManagedAccounts)
             .ToDictionary(x => x.Id, x => x.ManagedAccounts.First().Address);
    }

    [Activity]
    public virtual async Task UpdateDestinationRouteStatusAsync(
      IEnumerable<int> routeIds,
      decimal balance)
    {
        var routes = await dbContext.Routes
            .Where(x => routeIds.Any(y => y == x.Id))
            .ToListAsync();

        foreach (var route in routes)
        {
            if (balance < route.MaxAmountInSource && route.Status == RouteStatus.Active)
            {
                route.Status = RouteStatus.Inactive;
                Log.Information("Route {RouteId} set to Inactive (balance {Balance} < {Max})",
                    route.Id, balance, route.MaxAmountInSource);
            }
            else if (balance >= route.MaxAmountInSource && route.Status == RouteStatus.Inactive)
            {
                route.Status = RouteStatus.Active;
                Log.Information("Route {RouteId} set to Active (balance {Balance} >= {Max})",
                    route.Id, balance, route.MaxAmountInSource);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
