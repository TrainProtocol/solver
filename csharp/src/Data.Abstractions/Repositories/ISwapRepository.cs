using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ISwapRepository
{
    Task<Swap?> GetAsync(string commitId);

    Task<List<Swap>> GetAllAsync(uint page = 1, uint size = 20);

    Task<List<string>> GetNonRefundedSwapIdsAsync();

    Task<Swap> CreateAsync(
        string commitId,
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

    Task<int> CreateSwapTransactionAsync(
        string networkName,
        int? swapId,
        TransactionType transactionType,
        string transactionHash,
        DateTimeOffset timestamp,
        string feeAmount);

    Task<int> CreateSwapMetricAsync(
        int swapId,
        string sourceNetwork,
        string sourceToken,
        string destinationNetwork,
        string destinationToken,
        decimal volumeInUsd,
        decimal profitInUsd);
}