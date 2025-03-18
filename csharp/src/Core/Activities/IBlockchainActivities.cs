using Train.Solver.Core.Models;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Activities;

public interface IBlockchainActivities
{
    string FormatAddress(string address);

    bool ValidateAddress(string address);

    Task<string> GenerateAddressAsync(string networkName);

    Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset);

    Task EnsureSufficientBalanceAsync(string networkName, string address, string asset, decimal amount);

    Task<string> GetSpenderAddressAsync(string networkName, string asset);

    Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName);

    Task<Fee> EstimateFeeAsync(string networkName, EstimateFeeRequest request);

    Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request);

    Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock);

    Task<string> GetNonceAsync(string networkName, string address, string referenceId);

    Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args);

    Task<TransactionModel> GetTransactionAsync(string network, string transactionId);
}
