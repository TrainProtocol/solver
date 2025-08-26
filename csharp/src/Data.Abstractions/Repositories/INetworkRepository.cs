using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface INetworkRepository
{
    Task<Network?> GetAsync(string networkName);

    Task<Token?> GetTokenAsync(string networkName, string symbol);

    Task<IEnumerable<Network>> GetAllAsync(NetworkType[]? filterTypes);

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
        string nativeTokenPriceSymbol,
        string? nativeTokenContract,
        int nativeTokenDecimals,
        string nodeUrl,
        string nodeProvider);

    Task<Node?> CreateNodeAsync(
        string networkName,
        string providerName,
        string url);

    Task<Token?> CreateTokenAsync(
       string networkName,
       string symbol,
       string priceSymbol,
       string? contract,
       int decimals);

    Task DeleteTokenAsync(string networkName, string symbol);

    Task DeleteNodeAsync(string networkName, string providerName);
    Task<Network?> UpdateAsync(string networkName, string displayName, TransactionFeeType feeType, int feePercentageIncrease, string htlcNativeContractAddress, string htlcTokenContractAddress);
}