using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System.Security.Cryptography;
using Temporalio.Activities;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Models.HTLCModels;
using Train.Solver.Core.Repositories;
using Train.Solver.Core.Services;
using TransactionResponse = Train.Solver.Core.Models.TransactionResponse;

namespace Train.Solver.Core.Workflows.Activities;

public class SwapActivities(
    ISwapRepository swapRepository,
    INetworkRepository networkRepository,
    IFeeRepository feeRepository,
    IRouteService routeService)
{
    [Activity]
    public virtual async Task<string> CreateSwapAsync(
        HTLCCommitEventMessage commitEventMessage,
        decimal outputAmount,
        decimal feeAmount,
        string hashlock)
    {
        var swap = await swapRepository.CreateAsync(
            commitEventMessage.Id,
            commitEventMessage.SenderAddress,
            commitEventMessage.DestinationAddress,
            commitEventMessage.SourceNetwork,
            commitEventMessage.SourceAsset,
            commitEventMessage.Amount,
            commitEventMessage.DestinationNetwork,
            commitEventMessage.DestinationAsset,
            outputAmount,
            hashlock,
            feeAmount);

        return swap.Id;
    }

    [Activity]
    public virtual async Task<Dictionary<string, string>> GetSolverAddressesAsync(params string[] networkNames)
    {
        return await networkRepository.GetSolverAccountsAsync(networkNames);
    }

    [Activity]
    public virtual async Task<Guid> CreateSwapReferenceTransactionAsync(
        string networkName, string? swapId, TransactionType type)
    {
        var transaction = await swapRepository.InitiateSwapTransactionAsync(networkName, swapId, type);
        return transaction.Id;
    }

    [Activity]
    public virtual async Task UpdateSwapReferenceTransactionAsync(Guid id, TransactionResponse confirmedTransaction)
    {
        await swapRepository.UpdateSwapTransactionAsync(
            id,
            confirmedTransaction.TransactionHash,
            confirmedTransaction.Asset,
            confirmedTransaction.Amount,
            confirmedTransaction.Confirmations,
            confirmedTransaction.Timestamp,
            confirmedTransaction.FeeAsset,
            confirmedTransaction.FeeAmount);
    }

    [Activity]
    public virtual async Task<LimitModel> GetLimitAsync(SourceDestinationRequest request)
    {
        var limitResult = await routeService.GetLimitAsync(request);

        if (limitResult is null)
        {
            throw new RouteNotFoundException("Route not found");
        }

        return limitResult;
    }

    [Activity]
    public virtual async Task<QuoteModel> GetQuoteAsync(QuoteRequest request)
    {
        var quoteResult = await routeService.GetQuoteAsync(request);

        if (quoteResult is null)
        {
            throw new("Quote is null");
        }

        return quoteResult;
    }

    [Activity]
    public virtual async Task<HashlockModel> GenerateHashlockAsync()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var secretHex = bytes.ToHex(prefix: true);

        var secret = new HexBigInteger(secretHex).Value.ToString();

        var firstHash = SHA256.HashData(bytes);

        var hashHex = firstHash.ToHex(prefix: true);

        return await Task.FromResult(new HashlockModel(secret, hashHex));
    }

    [Activity]
    public virtual async Task UpdateExpensesAsync(
        string networkName,
        string feeAsset,
        decimal currentFee,
        string callDataAsset,
        TransactionType callDataType)
    {
        await feeRepository.UpdateExpenseAsync(
            networkName,
            callDataAsset,
            feeAsset,
            currentFee,
            callDataType);
    }
   
    [Activity]
    public async Task<List<string>> GetNonRefundedSwapIdsAsync()
    {
        return await swapRepository.GetNonRefundedSwapIdsAsync();
    }
}
