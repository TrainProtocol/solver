using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Workflows.Activities;

namespace Train.Solver.Blockchains.EVM.Activities;

public interface IEVMBlockchainActivities : IBlockchainActivities
{
    Task<Fee> IncreaseFeeAsync(EVMFeeIncreaseRequest request);

    Task<string> PublishRawTransactionAsync(EVMPublishTransactionRequest request);

    Task<TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);

    Task<SignedTransaction> ComposeSignedRawTransactionAsync(EVMComposeTransactionRequest request);

    Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request);
}
