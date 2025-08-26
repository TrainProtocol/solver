using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions.Cache;

public interface IBalanceCache
{
    Task SetAsync(string address, NetworkBalanceDto dto, TimeSpan ttl);

    Task<NetworkBalanceDto?> GetAsync(string address, string networkName);

    Task<IReadOnlyDictionary<string, NetworkBalanceDto>> GetAllAsync(string address);
}
