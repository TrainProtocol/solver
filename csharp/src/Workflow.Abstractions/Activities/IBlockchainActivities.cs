using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface IBlockchainActivities
{
    [Activity]
    Task<BalanceResponse> GetBalanceAsync(BalanceRequest request);
}

public interface IEventListenerActivities
{
    [Activity]
    Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request);

    [Activity]
    Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request);
}

public interface IAddLockSigActivities
{
    [Activity]
    Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request);
}

public interface ITransactionBuilderActivities
{
    [Activity]
    Task<PrepareTransactionDto> BuildTransactionAsync(TransactionBuilderRequest request);
}
