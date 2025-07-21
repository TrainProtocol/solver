using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System.Security.Cryptography;
using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using TransactionResponse = Train.Solver.Workflow.Abstractions.Models.TransactionResponse;
using Train.Solver.Common.Enums;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Swap.Activities;

public class SwapActivities(
    ISwapRepository swapRepository,
    IFeeRepository feeRepository,
    IRouteRepository routeRepository,
    IQuoteService routeService) : ISwapActivities
{
    [Activity]
    public virtual async Task<int> CreateSwapAsync(
        HTLCCommitEventMessage commitEventMessage,
        string outputAmount,
        string feeAmount,
        string hashlock)
    {
        var swap = await swapRepository.CreateAsync(
            commitEventMessage.CommitId,
            commitEventMessage.SenderAddress,
            commitEventMessage.DestinationAddress,
            commitEventMessage.SourceNetwork,
            commitEventMessage.SourceAsset,
            commitEventMessage.Amount.ToString(),
            commitEventMessage.DestinationNetwork,
            commitEventMessage.DestinationAsset,
            outputAmount,
            hashlock,
            feeAmount);

        return swap.Id;
    }

    [Activity]
    public virtual async Task<string[]> GetRouteSourceWalletsAsync(NetworkType type)
    {
        var routes = await routeRepository.GetAllAsync([RouteStatus.Active]);

        var wallets = routes
            .Where(x => x.SourceWallet.NetworkType == type)
            .Select(x => x.SourceWallet.Address)
            .Distinct()
            .ToArray();

        return wallets;
    }

    [Activity]
    public virtual async Task<int> CreateSwapTransactionAsync(int? swapId, TransactionType transactionType, TransactionResponse transaction)
    {
        return await swapRepository.CreateSwapTransactionAsync(
            transaction.NetworkName,
            swapId,
            transactionType,
            transaction.TransactionHash,
            transaction.Asset,
            transaction.Amount.ToString(),
            transaction.Confirmations,
            transaction.Timestamp,
            transaction.FeeAsset,
            transaction.Amount.ToString());
    }

    [Activity]
    public virtual async Task<LimitDto> GetLimitAsync(SourceDestinationRequest request)
    {
        var limitResult = await routeService.GetLimitAsync(request);

        if (limitResult is null)
        {
            throw new RouteNotFoundException("Route not found");
        }

        return limitResult;
    }

    [Activity]
    public virtual async Task<QuoteWithSolverDto> GetQuoteAsync(QuoteRequest request)
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
        string currentFee,
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
