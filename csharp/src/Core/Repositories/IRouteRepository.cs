using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync();

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        decimal? amount);
}
