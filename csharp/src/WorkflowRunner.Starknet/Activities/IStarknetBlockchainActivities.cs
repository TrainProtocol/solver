using Train.Solver.Blockchains.Starknet.Models;
using Train.Solver.Core.Models;
using Train.Solver.Core.Workflows.Activities;

namespace Train.Solver.Blockchains.Starknet.Activities;

public interface IStarknetBlockchainActivities : IBlockchainActivities
{
    Task<string> SimulateTransactionAsync(StarknetPublishTransactionRequest request);

    Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request);

    Task<string> PublishTransactionAsync(StarknetPublishTransactionRequest request);

    Task<Core.Models.TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);
}
