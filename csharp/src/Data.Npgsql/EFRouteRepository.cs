using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Util;

namespace Train.Solver.Data.Npgsql;

public class EFRouteRepository(SolverDbContext dbContext) : IRouteRepository
{
    public Task<List<Route>> GetAllAsync(RouteStatus[] statuses)
    {
        return GetBaseQuery(statuses).ToListAsync();
    }

    public async Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        string? amount)
    {
        var query = GetBaseQuery([RouteStatus.Active]);

        query = query.Where(x =>
            x.SourceToken.Asset == sourceToken
            && x.SourceToken.Network.Name == sourceNetworkName
            && x.DestinationToken.Asset == destinationToken
            && x.DestinationToken.Network.Name == destinationNetworkName);

        var route = await query.FirstOrDefaultAsync();

        if (route == null)
        {
            return null;
        }

        if (amount != null)
        {
            var routeMaxAmount = route.MaxAmountInSource;

            if (BigInteger.Parse(amount) > TokenUnitConverter.ToBaseUnits(routeMaxAmount, route.SourceToken.Decimals))
            {
                return null;
            }
        }

        return route;
    }

    public async Task<List<int>> GetReachablePointsAsync(RouteStatus[] statuses, bool fromSrcToDest, int? tokenId)
    {
        var reachablePoints = await dbContext.Routes
            .Where(x =>
                x.MaxAmountInSource > 0
                && statuses.Contains(x.Status)
                && (tokenId == null || (fromSrcToDest ? x.SourceTokenId == tokenId : x.DestinationTokenId == tokenId)))
            .Select(x => fromSrcToDest ? x.DestinationTokenId : x.SourceTokenId)
            .Distinct()
            .ToListAsync();

        return reachablePoints;
    }

    public async Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status)
    {
        var routes = await dbContext.Routes
            .Where(x => ids.Any(y => y == x.Id))
            .ToListAsync();

        foreach (var route in routes)
        {
            route.Status = status;
        }

        await dbContext.SaveChangesAsync();
    }

    private IQueryable<Route> GetBaseQuery(RouteStatus[] statuses)
        => dbContext.Routes
            .Include(x => x.SourceToken.Network.Nodes)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => x.MaxAmountInSource > 0 && statuses.Contains(x.Status));
}