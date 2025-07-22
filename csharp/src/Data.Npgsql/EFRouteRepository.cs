using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;

namespace Train.Solver.Data.Npgsql;

public class EFRouteRepository(
    SolverDbContext dbContext,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository,
    IWalletRepository walletRepository) : IRouteRepository
{
    public async Task<Route?> CreateAsync(
        string sourceNetworkName,
        string sourceTokenSymbol,
        string sourceWalletAddress,
        NetworkType sourceWalletType,
        string destinationNetworkName,
        string destinationTokenSymbol,
        string destinationWalletAddress,
        NetworkType destinationWalletType,
        string rateProviderName,
        BigInteger minAmount,
        BigInteger maxAmount,
        string? serviceFeeName)
    {
        var sourceToken = await networkRepository.GetTokenAsync(sourceNetworkName, sourceTokenSymbol);

        if (sourceToken == null)
        {
            throw new ArgumentException($"Source token {sourceTokenSymbol} not found in network {sourceNetworkName}");
        }

        var destinationToken = await networkRepository.GetTokenAsync(destinationNetworkName, destinationTokenSymbol);

        if (destinationToken == null)
        {
            throw new ArgumentException($"Destination token {destinationTokenSymbol} not found in network {destinationNetworkName}");
        }

        var sourceWallet = await walletRepository.GetAsync(sourceWalletType, sourceWalletAddress);

        if (sourceWallet == null)
        {
            throw new ArgumentException($"Source wallet {sourceWalletAddress} not found for network {sourceNetworkName}");
        }

        var destinationWallet = await walletRepository.GetAsync(destinationWalletType, destinationWalletAddress);

        if (destinationWallet == null)
        {
            throw new ArgumentException($"Destination wallet {destinationWalletAddress} not found for network {destinationNetworkName}");
        }

        var rateProvider = await dbContext.RateProviders
            .FirstOrDefaultAsync(x => x.Name == rateProviderName);

        if (rateProvider == null)
        {
            throw new ArgumentException($"Rate provider {rateProviderName} not found");
        }

        var route = new Route
        {
            SourceToken = sourceToken,
            DestinationToken = destinationToken,
            SourceWallet = sourceWallet,
            DestinationWallet = destinationWallet,
            RateProvider = rateProvider,
            MinAmountInSource = minAmount.ToString(),
            MaxAmountInSource = maxAmount.ToString(),
            Status = RouteStatus.Active,
        };

        var serviceFee = string.IsNullOrEmpty(serviceFeeName)
            ? null
            : await feeRepository.GetServiceFeeAsync(serviceFeeName);

        if (serviceFee != null)
        {
            route.ServiceFeeId = serviceFee.Id;
        }

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();

        return route;
    }

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

            if (amount > BigInteger.Parse(routeMaxAmount))
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
            .Include(x => x.RateProvider)
            .Include(x => x.SourceWallet)
            .Include(x => x.DestinationWallet)
            .Include(x => x.ServiceFee)
            .Include(x => x.SourceToken.Network.Nodes)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => statuses.Contains(x.Status));
}