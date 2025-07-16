using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Enums;

namespace Train.Solver.Blockchain.Abstractions.Activities;
public interface ISwapActivities
{
    [Activity]
    Task<int> CreateSwapAsync(HTLCCommitEventMessage commitEventMessage, string outputAmount, string feeAmount, string hashlock);

    [Activity]
    Task<int> CreateSwapTransactionAsync(int? swapId, TransactionType transactionType, TransactionResponse transaction);

    [Activity]
    Task<HashlockModel> GenerateHashlockAsync();

    [Activity]
    Task<LimitDto> GetLimitAsync(SourceDestinationRequest request);

    [Activity]
    Task<List<string>> GetNonRefundedSwapIdsAsync();

    [Activity]
    Task<QuoteDto> GetQuoteAsync(QuoteRequest request);

    [Activity]
    Task<string> GetSolverAddressAsync(NetworkType type);

    [Activity]
    Task UpdateExpensesAsync(string networkName, string feeAsset, string currentFee, string callDataAsset, TransactionType callDataType);
}