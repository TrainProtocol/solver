using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Activities;

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
}
