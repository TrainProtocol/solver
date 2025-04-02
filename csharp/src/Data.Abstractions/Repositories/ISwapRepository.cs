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
        decimal sourceAmount,
        string destinationNetworkName,
        string destinationToken,
        decimal destinationAmount,
        string hashlock,
        decimal feeAmount);

    Task<Guid> CreateSwapTransactionAsync(
        string networkName,
        string swapId,
        TransactionType transactionType,
        string transactionHash,
        string asset,
        decimal amount,
        int confirmations,
        DateTimeOffset timestamp,
        string feeAsset,
        decimal feeAmount);
}