using Train.Solver.Core.Activities;
using Train.Solver.Core.Blockchains.EVM.Models;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Blockchains.EVM.Activities;

public interface IEVMBlockchainActivities : IBlockchainActivities
{
    Fee IncreaseFee(Fee requestFee, int feeIncreasePercentage);

    Task<string> PublishRawTransactionAsync(
        string networkName,
        string fromAddress,
        SignedTransaction signedTransaction);

    Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds);

    Task<SignedTransaction> ComposeSignedRawTransactionAsync(
        string networkName,
        string fromAddress,
        string toAddress,
        string nonce,
        string amountInWei,
        string? callData,
        Fee fee);

    Task<decimal> GetSpenderAllowanceAsync(
        string networkName, string ownerAddress, string spenderAddress, string asset);
}
