using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System.Security.Cryptography;
using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;
using TransactionResponse = Train.Solver.Blockchain.Abstractions.Models.TransactionResponse;
using Train.Solver.Util.Enums;

namespace Train.Solver.Blockchain.Swap.Activities;

public class SwapActivities(
    ISwapRepository swapRepository,
    IWalletRepository walletRepository,
    IFeeRepository feeRepository,
    IRouteService routeService) : ISwapActivities
{
    [Activity]
    public virtual async Task<int> CreateSwapAsync(
        HTLCCommitEventMessage commitEventMessage,
        string outputAmount,
        string feeAmount,
        string hashlock)
    {
        var swap = await swapRepository.CreateAsync(
            commitEventMessage.Id,
            commitEventMessage.SenderAddress,
            commitEventMessage.DestinationAddress,
            commitEventMessage.SourceNetwork,
            commitEventMessage.SourceAsset,
            commitEventMessage.AmountInWei,
            commitEventMessage.DestinationNetwork,
            commitEventMessage.DestinationAsset,
            outputAmount,
            hashlock,
            feeAmount);

        return swap.Id;
    }

    [Activity]
    public virtual async Task<string> GetSolverAddressAsync(NetworkType type)
    {

        var wallet = await walletRepository.GetDefaultAsync(type);

        if (wallet == null)
        {
            throw new ArgumentNullException(nameof(wallet));
        }

        return wallet.Address;
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
            transaction.Amount,
            transaction.Confirmations,
            transaction.Timestamp,
            transaction.FeeAsset,
            transaction.FeeAmount);
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
    public virtual async Task<QuoteDto> GetQuoteAsync(QuoteRequest request)
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
