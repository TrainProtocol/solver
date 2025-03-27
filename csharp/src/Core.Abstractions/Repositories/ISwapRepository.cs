using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.Core.Abstractions.Repositories;

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

    Task<Transaction> InitiateSwapTransactionAsync(
        string networkName,
        string swapId,
        TransactionType transactionType);

    Task<Guid> UpdateSwapTransactionAsync(
        Guid transactionId,
        string transactionHash,
        string token,
        decimal amount,
        int confirmations,
        DateTimeOffset timestamp,
        string feeToken,
        decimal feeAmount);

    Task<Transaction?> GetSwapTransactionAsync(Guid transactionId);

    Task<ReservedNonce?> GetSwapTransactionReservedNonceAsync(Guid transactionId);

    Task<ReservedNonce> CreateSwapTransactionReservedNonceAsync(string networkName, Guid transactionId, string nonce);
}