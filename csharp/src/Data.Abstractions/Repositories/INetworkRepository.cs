using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

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
        string htlcTokenContractAddress,
        string nativeTokenSymbol,
        string nativeTokenContract,
        int nativeTokenDecimals);

    Task<Node?> CreateNodeAsync(
        string networkName,
        string url);

    Task<Token?> CreateTokenAsync(
       string networkName,
       string symbol,
       string? contract,
       int decimals);

    Task<Token?> CreateNativeTokenAsync(
        string networkName,
        string symbol,
        int decimals);

    Task DeleteTokenAsync(string networkName, string symbol);

    Task<Token?> GetTokenAsync(string networkName, string asset);


    Task<List<Token>> GetTokensAsync();

    Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices);
}