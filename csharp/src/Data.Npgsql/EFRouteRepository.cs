using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

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
        decimal? amount)
    {
        var query = GetBaseQuery([RouteStatus.Active]);

        query = query.Where(x =>
            x.SourceToken.Asset == sourceToken
            && x.SourceToken.Network.Name == sourceNetworkName
            && x.DestinationToken.Asset == destinationToken
            && x.DestinationToken.Network.Name == destinationNetworkName);

        if (amount.HasValue)
        {
            query = query.Where(x => amount <= x.MaxAmountInSource);
        }

        return await query.FirstOrDefaultAsync();
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
            .Include(x => x.SourceToken.Network.ManagedAccounts)
            .Include(x => x.SourceToken.Network.Contracts)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes)
            .Include(x => x.DestinationToken.Network.ManagedAccounts)
            .Include(x => x.DestinationToken.Network.Contracts)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => x.MaxAmountInSource > 0 && statuses.Contains(x.Status));
}