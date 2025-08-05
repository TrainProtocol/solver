using Temporalio.Activities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.Models;

namespace Train.Solver.Workflow.EVM.Activities;

public interface IEVMBlockchainActivities
{
    [Activity]
    Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);

    [Activity]
    Task<string> GetNextNonceAsync(NextNonceRequest request);

    [Activity]
    Task<Fee> IncreaseFeeAsync(EVMFeeIncreaseRequest request);

    [Activity]
    Task<string> PublishRawTransactionAsync(EVMPublishTransactionRequest request);

    [Activity]
    Task<TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);

    [Activity]
    Task<SignedTransaction> ComposeSignedRawTransactionAsync(EVMComposeTransactionRequest request);

    [Activity]
    Task<string> GetSpenderAllowanceAsync(AllowanceRequest request);
}
