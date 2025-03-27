using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.Core.Abstractions.Repositories;

public interface INetworkRepository
{
    Task<Network?> GetAsync(string networkName);

    Task<List<Network>> GetAllAsync();

    Task<Token?> GetTokenAsync(string networkName, string asset);

    Task<List<Token>> GetTokensAsync();

    Task<Dictionary<string, Token>> GetNativeTokensAsync(string[] networkNames);

    Task<Dictionary<string, string>> GetSolverAccountsAsync(string[] networkNames);

    Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices);
}
