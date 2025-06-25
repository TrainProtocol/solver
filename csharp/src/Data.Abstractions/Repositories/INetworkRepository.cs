using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface INetworkRepository
{
    Task<Network?> GetAsync(string networkName);

    Task<List<Network>> GetAllAsync();

    Task<Token?> GetTokenAsync(string networkName, string asset);

    Task<List<Token>> GetTokensAsync();

    Task<List<Token>> GetTokensAsync(int[] ids);

    Task<string> GetSolverAccountAsync(string networkName);

    Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices);
}
