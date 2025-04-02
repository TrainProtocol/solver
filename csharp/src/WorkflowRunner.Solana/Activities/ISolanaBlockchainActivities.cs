using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Solana.Models;

namespace Train.Solver.Blockchain.Solana.Activities;

public interface ISolanaBlockchainActivities : IBlockchainActivities
{
    Task SimulateTransactionAsync(SolanaPublishTransactionRequest request);

    Task<byte[]> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request);

    Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request);
}
