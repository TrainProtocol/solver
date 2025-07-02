using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ISwapRepository
{
    Task<Swap?> GetAsync(string id);

    Task<List<Swap>> GetAllAsync(uint page = 1, uint size = 20, string[]? addresses = null);

    Task<List<string>> GetNonRefundedSwapIdsAsync();

    Task<Swap> CreateAsync(
        string id,
        string senderAddress,
        string destinationAddress,
        string sourceNetworkName,
        string sourceToken,
        string sourceAmount,
        string destinationNetworkName,
        string destinationToken,
        string destinationAmount,
        string hashlock,
        string feeAmount);

    Task<Guid> CreateSwapTransactionAsync(
        string networkName,
        string swapId,
        TransactionType transactionType,
        string transactionHash,
        string asset,
        string amount,
        int confirmations,
        DateTimeOffset timestamp,
        string feeAsset,
        string feeAmount);
}