using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.WorkflowRunner.Solana.Models;

namespace Train.Solver.WorkflowRunner.Solana.Activities;

public interface ISolanaBlockchainActivities : IBlockchainActivities
{
    Task SimulateTransactionAsync(SolanaPublishTransactionRequest request);

    Task<byte[]> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request);

    Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request);
}
