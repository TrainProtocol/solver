using Train.Solver.Blockchains.Solana.Models;
using Train.Solver.Core.Workflows.Activities;

namespace Train.Solver.Blockchains.Solana.Activities;

public interface ISolanaBlockchainActivities : IBlockchainActivities
{
    Task SimulateTransactionAsync(SolanaPublishTransactionRequest request);

    Task<byte[]> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request);

    Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request);
}
