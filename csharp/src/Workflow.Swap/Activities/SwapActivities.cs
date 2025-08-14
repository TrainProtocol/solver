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
using System.Numerics;
using Train.Solver.Common.Helpers;
using Train.Solver.Infrastructure.Extensions;

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
            transaction.Timestamp,
            transaction.FeeAmount.ToString());
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
    public async Task<List<DetailedSwapDto>> GetNonRefundedSwapsAsync()
    {
        var swaps = await swapRepository.GetNonRefundedSwapsAsync();
        return swaps.Select(x => x.ToDetailedDto()).ToList();
    }

    [Activity]
    public async Task CreateSwapMetricAsync(
    string commitId,
    QuoteDto quote)
    {
        var swap = await swapRepository.GetAsync(commitId);

        if (swap is null)
            throw new Exception($"Swap with commitId {commitId} not found.");

        var sourceAmount = BigInteger.Parse(swap.SourceAmount);

        var volume = TokenUnitHelper.FromBaseUnits(sourceAmount, swap.Route.SourceToken.Decimals);
        var volumeInUsd = volume * swap.Route.SourceToken.TokenPrice.PriceInUsd;

        var serviceFee = TokenUnitHelper.FromBaseUnits(quote.TotalServiceFee, swap.Route.SourceToken.Decimals);
        var estimatedExpense = TokenUnitHelper.FromBaseUnits(quote.TotalExpenseFee, swap.Route.SourceToken.Decimals);

        var collectedFeeUsd = (serviceFee + estimatedExpense) * swap.Route.SourceToken.TokenPrice.PriceInUsd;

        decimal actualExpenseUsd = 0;
        foreach (var tx in swap.Transactions)
        {
            if (BigInteger.TryParse(tx.FeeAmount, out var feeAmount) &&
                tx.Network?.NativeToken?.TokenPrice != null)
            {
                var feeInUnits = TokenUnitHelper.FromBaseUnits(feeAmount, tx.Network.NativeToken.Decimals);
                actualExpenseUsd += feeInUnits * tx.Network.NativeToken.TokenPrice.PriceInUsd;
            }
        }

        var profitInUsd = collectedFeeUsd - actualExpenseUsd;

        await swapRepository.CreateSwapMetricAsync(
            swap.Id,
            swap.Route.SourceToken.Network.Name,
            swap.Route.SourceToken.Asset,
            swap.Route.DestinationToken.Network.Name,
            swap.Route.DestinationToken.Asset,
            volumeInUsd,
            profitInUsd);
    }
}
