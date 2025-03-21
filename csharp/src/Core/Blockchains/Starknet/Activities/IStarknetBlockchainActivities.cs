using Train.Solver.Core.Activities;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Blockchains.Starknet.Activities;

public interface IStarknetBlockchainActivities : IBlockchainActivities
{
    Task<string> SimulateTransactionAsync(
        string fromAddress,
        string networkName,
        string? nonce,
        string? callData,
        Fee? fee);

    Task<decimal> GetSpenderAllowanceAsync(
        string networkName, string ownerAddress, string spenderAddress, string asset);

    Task<string> PublishTransactionAsync(
        string fromAddress,
        string networkName,
        string? nonce,
        string? callData,
        Fee? fee);

    Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds);
}
