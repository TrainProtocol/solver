using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync(RouteStatus[] statuses);

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        decimal? amount);

    Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status);
}
