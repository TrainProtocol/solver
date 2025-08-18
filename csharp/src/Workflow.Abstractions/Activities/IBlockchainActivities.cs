using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface IBlockchainActivities
{
    [Activity]
    Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);

    [Activity]
    Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);

    [Activity]
    Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request);

    [Activity]
    Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);

    [Activity]
    Task<PrepareTransactionDto> BuildTransactionAsync(TransactionBuilderRequest request);

    [Activity]
    Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);
}
