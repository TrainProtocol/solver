using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Util.Enums;
using Train.Solver.Util.Helpers;

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
        BigInteger? amount)
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

            if (amount > TokenUnitHelper.ToBaseUnits(routeMaxAmount, route.SourceToken.Decimals))
            {
                return null;
            }
        }

        return route;
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
            .Include(x => x.SourceWallet)
            .Include(x => x.DestinationWallet)
            .Include(x => x.ServiceFee)
            .Include(x => x.SourceToken.Network.Nodes)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => x.MaxAmountInSource > 0 && statuses.Contains(x.Status));
}