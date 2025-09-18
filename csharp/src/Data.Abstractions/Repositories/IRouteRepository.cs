using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync(RouteStatus[]? statuses);

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken);

    Task<Route?> CreateAsync(CreateRouteRequest request);

    Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status);

    Task<List<RateProvider>> GetAllRateProvidersAsync();

    Task<Route?> UpdateAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken, 
        UpdateRouteRequest request);
}
