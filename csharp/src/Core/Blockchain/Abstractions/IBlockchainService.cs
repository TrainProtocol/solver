using FluentResults;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.Abstractions;

public class BlockNumberResponse
{
    public string BlockNumber { get; set; } = null!;

    public string? BlockHash { get; set; }
}

public interface IBlockchainService
{
    Task<Result<PrepareTransactionResponse>> BuildTransactionAsync(
        string networkName,
        TransactionType transactionType,
        string args);

    string FormatAddress(string address);

    bool ValidateAddress(string address);

    Task<Result<string>> GenerateAddressAsync(string networkName);

    Task<Result<BalanceResponse>> GetBalanceAsync(string networkName, string address, string asset);

    Task<Result<string>> GetSpenderAllowanceAsync(
        string networkName,
        string ownerAddress,
        string spenderAddress,
        string asset);

    Task<Result<BlockNumberResponse>> GetLastConfirmedBlockNumberAsync(string networkName);

    Task<Result<Fee>> EstimateFeeAsync(
        string network,
        string asset,
        string fromAddress,
        string toAddress,
        decimal amount,
        string? data = null);

    Task<Result<bool>> ValidateAddLockSignatureAsync(string networkName, AddLockSigValidateRequest request);

    Task<Result<HTLCBlockEvent>> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock);

    Task<Result<string>> GetReservedNonceAsync(
       string networkName,
       string address,
       string referenceId);

    Task<Result<string>> GetNextNonceAsync(
        string networkName,
        string address,
        string referenceId);

    Task<Result<TransactionReceiptModel>> GetConfirmedTransactionAsync(
       string network,
       string transactionId);

}
