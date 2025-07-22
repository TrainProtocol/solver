using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Workflow.Abstractions.Models;
using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Activities;
public interface ISwapActivities
{
    [Activity]
    Task<int> CreateSwapAsync(HTLCCommitEventMessage commitEventMessage, string outputAmount, string feeAmount, string hashlock);
    
    [Activity]
    Task CreateSwapMetricAsync(string commitId, BigInteger totalServiceFee);
    
    [Activity]
    Task<int> CreateSwapTransactionAsync(int? swapId, TransactionType transactionType, TransactionResponse transaction);

    [Activity]
    Task<HashlockModel> GenerateHashlockAsync();

    [Activity]
    Task<LimitDto> GetLimitAsync(SourceDestinationRequest request);

    [Activity]
    Task<List<string>> GetNonRefundedSwapIdsAsync();

    [Activity]
    Task<QuoteWithSolverDto> GetQuoteAsync(QuoteRequest request);

    [Activity]
    Task<string[]> GetRouteSourceWalletsAsync(NetworkType type);

    [Activity]
    Task UpdateExpensesAsync(string networkName, string feeAsset, string currentFee, string callDataAsset, TransactionType callDataType);
}