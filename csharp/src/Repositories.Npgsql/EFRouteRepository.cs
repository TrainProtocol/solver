using Microsoft.EntityFrameworkCore;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Repositories;

namespace Train.Solver.Repositories.Npgsql;

public class EFRouteRepository(SolverDbContext dbContext) : IRouteRepository
{
    public Task<List<Route>> GetAllAsync()
    {
        return GetBaseQuery().ToListAsync();
    }

    public async Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        decimal? amount)
    {
        var query = GetBaseQuery();

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

    private IQueryable<Route> GetBaseQuery()
        => dbContext.Routes
            .Include(x => x.SourceToken.Network.Nodes)
            .Include(x => x.SourceToken.Network.ManagedAccounts)
            .Include(x => x.SourceToken.Network.Contracts)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes)
            .Include(x => x.DestinationToken.Network.ManagedAccounts)
            .Include(x => x.DestinationToken.Network.Contracts)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => x.MaxAmountInSource > 0 && x.Status == RouteStatus.Active);
}