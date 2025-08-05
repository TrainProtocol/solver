using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync(RouteStatus[]? statuses);

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        BigInteger? maxAmount);

    Task<Route?> CreateAsync(
        string sourceNetworkName,
        string sourceToken,
        string sourceWalletAddress,
        NetworkType sourceWalletType,
        string destinationNetworkName,
        string destinationToken,
        string destinationWalletAddress,
        NetworkType destinationWalletType,
        string rateProvider,
        BigInteger minAmount,
        BigInteger maxAmount,
        string? serviceFee);

    Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status);

    Task<List<RateProvider>> GetAllRateProvidersAsync();

    Task<Route?> UpdateAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken, 
        string rateProviderName,
        BigInteger minAmount,
        BigInteger maxAmount,
        RouteStatus status, 
        string? serviceFeeName);
}
