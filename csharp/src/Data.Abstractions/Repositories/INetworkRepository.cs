using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface INetworkRepository
{
    Task<Network?> GetAsync(string networkName);

    Task<IEnumerable<Network>> GetAllAsync();

    Task<Network?> CreateAsync(
        string networkName,
        string displayName,
        NetworkType type,
        TransactionFeeType feeType,
        string chainId,
        int feePercentageIncrease,
        string htlcNativeContractAddress,
        string htlcTokenContractAddress);

    Task<Token?> GetTokenAsync(string networkName, string asset);


    Task<List<Token>> GetTokensAsync();

    Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices);
}