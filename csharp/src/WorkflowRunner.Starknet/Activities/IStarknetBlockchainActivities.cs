using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Starknet.Models;

namespace Train.Solver.Blockchain.Starknet.Activities;

public interface IStarknetBlockchainActivities : IBlockchainActivities
{
    Task<string> SimulateTransactionAsync(StarknetPublishTransactionRequest request);

    Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request);

    Task<string> PublishTransactionAsync(StarknetPublishTransactionRequest request);

    Task<Abstractions.Models.TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request);
}
