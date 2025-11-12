using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Npgsql;

public class EFRouteRepository(
    SolverDbContext dbContext,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository,
    IWalletRepository walletRepository) : IRouteRepository
{
    public async Task<Route?> CreateAsync(CreateRouteRequest request)
    {
        var sourceToken = await networkRepository.GetTokenAsync(request.SourceNetworkName, request.SourceToken);

        if (sourceToken == null)
        {
            throw new ArgumentException($"Source token {request.SourceToken} not found in network {request.SourceNetworkName}");
        }

        var destinationToken = await networkRepository.GetTokenAsync(request.DestinationNetworkName, request.DestinationToken);

        if (destinationToken == null)
        {
            throw new ArgumentException($"Destination token {request.DestinationToken} not found in network {request.DestinationNetworkName}");
        }

        var sourceWallet = await walletRepository.GetAsync(request.SourceWalletType, request.SourceWalletAddress);

        if (sourceWallet == null)
        {
            throw new ArgumentException($"Source wallet {request.SourceWalletAddress} not found for network {request.SourceNetworkName}");
        }

        var destinationWallet = await walletRepository.GetAsync(request.DestinationWalletType, request.DestinationWalletAddress);

        if (destinationWallet == null)
        {
            throw new ArgumentException($"Destination wallet {request.DestinationWalletAddress} not found for network {request.DestinationNetworkName}");
        }

        var rateProvider = await dbContext.RateProviders
            .FirstOrDefaultAsync(x => x.Name == request.RateProvider);

        if (rateProvider == null)
        {
            throw new ArgumentException($"Rate provider {request.RateProvider} not found");
        }
       
        var serviceFee = await feeRepository.GetServiceFeeAsync(request.ServiceFee);

        if (serviceFee == null)
        {
            throw new ArgumentException($"Service fee {request.ServiceFee} not found");
        }

        var route = new Route
        {
            SourceToken = sourceToken,
            DestinationToken = destinationToken,
            SourceWallet = sourceWallet,
            DestinationWallet = destinationWallet,
            RateProvider = rateProvider,
            MinAmountInSource = request.MinAmount.ToString(),
            MaxAmountInSource = request.MaxAmount.ToString(),
            Status = RouteStatus.Active,
            IgnoreExpenseFee = request.IgnoreExpenseFee,
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
       UpdateRouteRequest request)
    {
        var route = await GetAsync(
            sourceNetworkName,
            sourceToken,
            destinationNetworkName,
            destinationToken);

        if (route == null)
        {
            throw new Exception("Route not found");
        }

        var rateProvider = await dbContext.RateProviders
            .FirstOrDefaultAsync(x => x.Name == request.RateProvider);

        if (rateProvider != null)
        {
            route.RateProviderId = rateProvider.Id;
        }

        var serviceFee = await feeRepository.GetServiceFeeAsync(request.ServiceFee);

        if (serviceFee == null)
        {
            throw new ArgumentException($"Service fee {request.ServiceFee} not found");
        }

        
        route.MinAmountInSource = request.MinAmount.ToString();
        route.MaxAmountInSource = request.MaxAmount.ToString();
        route.ServiceFee = serviceFee;
        route.IgnoreExpenseFee = request.IgnoreExpenseFee;
        route.Status = request.Status;

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
        string destinationToken)
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