using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Repositories;

public interface INetworkRepository
{
    Task<Network?> GetAsync(string networkName);

    Task<List<Network>> GetAllAsync();

    Task<Token?> GetTokenAsync(string networkName, string asset);

    Task<Token?> GetTokenByContractAsync(string networkName, string contractAddress);

    Task<Token?> GetNativeTokenAsync(string networkName);

    Task<Dictionary<string, Token>> GetNativeTokensAsync(string[] networkNames);

    Task<Dictionary<string, string>> GetSolverAccountsAsync(string[] networkNames);

    Task<List<Token>> GetTokensAsync();

    Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices);
}
