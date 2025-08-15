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
        bool ignoreExpenseFee,
        string serviceFeeName)
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
       
        var serviceFee = await feeRepository.GetServiceFeeAsync(serviceFeeName);

        if (serviceFee == null)
        {
            throw new ArgumentException($"Service fee {serviceFeeName} not found");
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
            IgnoreExpenseFee = ignoreExpenseFee,
            ServiceFee = serviceFee,
        };
       

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();

        return route;
    }

    public async Task<Route?> UpdateAsync(
       string sourceNetworkName,
       string sourceToken,
       string destinationNetworkName,
       string destinationToken,
       string rateProviderName,
       BigInteger minAmount,
       BigInteger maxAmount,
       RouteStatus status,
       bool ignoreExpenseFee,
       string serviceFeeName)
    {
        var route = await GetAsync(
            sourceNetworkName,
            sourceToken,
            destinationNetworkName,
            destinationToken,
            amount: null);

        if (route == null)
        {
            throw new Exception("Route not found");
        }

        var rateProvider = await dbContext.RateProviders
            .FirstOrDefaultAsync(x => x.Name == rateProviderName);

        if (rateProvider != null)
        {
            route.RateProviderId = rateProvider.Id;
        }

        var serviceFee = await feeRepository.GetServiceFeeAsync(serviceFeeName);

        if (serviceFee == null)
        {
            throw new ArgumentException($"Service fee {serviceFeeName} not found");
        }


        route.MinAmountInSource = minAmount.ToString();
        route.MaxAmountInSource = maxAmount.ToString();
        route.ServiceFee = serviceFee;
        route.IgnoreExpenseFee = ignoreExpenseFee;
        route.Status = status;

        await dbContext.SaveChangesAsync();

        return route;
    }

    public Task<List<Route>> GetAllAsync(RouteStatus[]? statuses)
    {
        return GetBaseQuery(statuses).ToListAsync();
    }

    public Task<List<RateProvider>> GetAllRateProvidersAsync()
    {
        return dbContext.RateProviders.ToListAsync();
    }

    public async Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        BigInteger? amount)
    {
        var query = GetBaseQuery(null);

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

    private IQueryable<Route> GetBaseQuery(RouteStatus[]? statuses)
        => dbContext.Routes
            .Include(x => x.RateProvider)
            .Include(x => x.SourceWallet.SignerAgent)
            .Include(x => x.DestinationWallet.SignerAgent)
            .Include(x => x.ServiceFee)
            .Include(x => x.SourceToken.Network.Nodes)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network.Nodes)
            .Include(x => x.DestinationToken.TokenPrice)
            .Where(x => statuses == null || statuses.Contains(x.Status));
}