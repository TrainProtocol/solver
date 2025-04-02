using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.Models;

namespace Train.Solver.Blockchain.EVM.Activities;

public interface IEVMBlockchainActivities : IBlockchainActivities
{
    Task<Fee> IncreaseFeeAsync(EVMFeeIncreaseRequest request);

    Task<string> PublishRawTransactionAsync(EVMPublishTransactionRequest request);

    Task<TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);

    Task<SignedTransaction> ComposeSignedRawTransactionAsync(EVMComposeTransactionRequest request);

    Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request);
}
