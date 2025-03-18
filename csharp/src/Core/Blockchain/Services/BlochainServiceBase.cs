using FluentResults;
using Microsoft.EntityFrameworkCore;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.Services;

public abstract class BlochainServiceBase(SolverDbContext dbContext) : IBlockchainService
{
    public abstract Task<Result<string>> GetNextNonceAsync(
        string networkName,
        string address,
        string referenceId);

    public abstract Task<Result<PrepareTransactionResponse>> BuildTransactionAsync(string networkName, TransactionType transactionType, string args);
    public abstract Task<Result<string>> GenerateAddressAsync(string networkName);
    public abstract Task<Result<Fee>> EstimateFeeAsync(string network, string asset, string fromAddress, string toAddress, decimal amount, string? data = null);
    public abstract Task<Result<BalanceResponse>> GetBalanceAsync(string networkName, string address, string asset);
    public abstract Task<Result<TransactionReceiptModel>> GetConfirmedTransactionAsync(string network, string transactionId);
    public abstract Task<Result<HTLCBlockEvent>> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock);
    public abstract Task<Result<BlockNumberResponse>> GetLastConfirmedBlockNumberAsync(string networkName);
    public abstract Task<Result<string>> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset);
    public abstract Task<Result<bool>> ValidateAddLockSignatureAsync(string networkName, AddLockSigValidateRequest request);
    public abstract string FormatAddress(string address);
    public abstract bool ValidateAddress(string address);
    
    public async Task<Result<string>> GetReservedNonceAsync(
        string networkName,
        string address,
        string referenceId)
    {
        var network = await dbContext.Networks
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain setup for {networkName} is missing");
        }

        var reservedNonce = await dbContext.ReservedNonces
             .Include(x => x.Network)
             .FirstOrDefaultAsync(x =>
                 x.NetworkId == network.Id
                 && x.ReferenceId == referenceId);

        if (reservedNonce is not null)
        {
            return Result.Ok(reservedNonce.Nonce);
        }

        var nextNonceResult = await GetNextNonceAsync(
            networkName,
            address,
            referenceId);

        if (nextNonceResult.IsFailed)
        {
            return nextNonceResult.ToResult();
        }

        reservedNonce = new()
        {
            ReferenceId = referenceId,
            Nonce = nextNonceResult.Value.ToString(),
            NetworkId = network.Id,
        };

        dbContext.Add(reservedNonce);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            return Result.Fail($"Nonce already reserved. Exception: {e.Message}");
        }

        return Result.Ok(reservedNonce.Nonce);
    }
}
