using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.Core.Abstractions.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync();

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        decimal? amount);

    Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status);
}
