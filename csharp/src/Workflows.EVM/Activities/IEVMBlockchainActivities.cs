using Temporalio.Activities;
using Train.Solver.Workflows.Abstractions.Models;
using Train.Solver.Workflows.EVM.Models;

namespace Train.Solver.Workflows.EVM.Activities;

public interface IEVMBlockchainActivities
{
    [Activity]
    Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);

    [Activity]
    Task<string> GetNextNonceAsync(NextNonceRequest request);

    [Activity]
    Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);

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
