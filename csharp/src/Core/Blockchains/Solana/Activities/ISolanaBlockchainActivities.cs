using Train.Solver.Core.Activities;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Blockchains.Solana.Activities;

public interface ISolanaBlockchainActivities : IBlockchainActivities
{
    Task SimulateTransactionAsync(
        string networkName,
        byte[] rawTx);

    Task<byte[]> ComposeSolanaTranscationAsync(
        Fee fee,
        string fromAddress,
        string callData,
        string lastValidBLockHash);

    Task<string> PublishTransactionAsync(
       string networkName,
       byte[] rawTx);
}
