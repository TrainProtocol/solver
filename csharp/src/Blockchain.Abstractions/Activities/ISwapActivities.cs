using Temporalio.Activities;
using Train.Solver.API.Models;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;
public interface ISwapActivities
{
    [Activity]
    Task<string> CreateSwapAsync(HTLCCommitEventMessage commitEventMessage, decimal outputAmount, decimal feeAmount, string hashlock);

    [Activity]
    Task<Guid> CreateSwapTransactionAsync(string swapId, TransactionType transactionType, TransactionResponse transaction);

    [Activity]
    Task<HashlockModel> GenerateHashlockAsync();

    [Activity]
    Task<LimitDto> GetLimitAsync(SourceDestinationRequest request);

    [Activity]
    Task<List<string>> GetNonRefundedSwapIdsAsync();

    [Activity]
    Task<QuoteDto> GetQuoteAsync(QuoteRequest request);

    [Activity]
    Task<Dictionary<string, string>> GetSolverAddressesAsync(params string[] networkNames);

    [Activity]
    Task UpdateExpensesAsync(string networkName, string feeAsset, decimal currentFee, string callDataAsset, TransactionType callDataType);
}