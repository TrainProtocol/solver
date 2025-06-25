using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync(RouteStatus[] statuses);

    Task<List<int>> GetReachablePointsAsync(RouteStatus[] statuses, bool fromSrcToDest, int? tokenId);

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        string? amount);

    Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status);
}
