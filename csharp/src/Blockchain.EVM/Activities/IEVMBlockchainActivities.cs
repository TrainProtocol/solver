using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.Models;

namespace Train.Solver.Blockchain.EVM.Activities;

public interface IEVMBlockchainActivities
{
    [Activity]
    Task<EVMFeeModel> EstimateFeeAsync(EstimateFeeRequest request);

    [Activity]
    Task<string> GetNextNonceAsync(NextNonceRequest request);

    [Activity]
    Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);

    [Activity]
    Task<EVMFeeModel> IncreaseFeeAsync(EVMFeeIncreaseRequest request);

    [Activity]
    Task<string> PublishRawTransactionAsync(EVMPublishTransactionRequest request);

    [Activity]
    Task<TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);

    [Activity]
    Task<SignedTransaction> ComposeSignedRawTransactionAsync(EVMComposeTransactionRequest request);

    [Activity]
    Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request);
}
