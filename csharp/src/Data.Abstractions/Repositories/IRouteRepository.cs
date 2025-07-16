using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync(RouteStatus[] statuses);

    Task<Route?> GetAsync(
        string sourceNetworkName,
        string sourceToken,
        string destinationNetworkName,
        string destinationToken,
        BigInteger? amount);

    Task UpdateRoutesStatusAsync(int[] ids, RouteStatus status);
}
