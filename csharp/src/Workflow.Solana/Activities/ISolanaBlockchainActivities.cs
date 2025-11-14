using Temporalio.Activities;
using Train.Solver.Blockchain.Solana.Models;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Solana.Models;

namespace Train.Solver.Workflow.Solana.Activities;

public interface ISolanaBlockchainActivities
{
    [Activity]
    Task<string> GetNextNonceAsync(NextNonceRequest request);

    [Activity]
    Task<PrepareTransactionDto> BuildTransactionAsync(TransactionBuilderRequest request);

    [Activity]
    Task<TransactionResponse> GetTransactionAsync(DetailedNetworkDto network, string transactionId);

    [Activity]
    Task SimulateTransactionAsync(SolanaPublishTransactionRequest request);

    [Activity]
    Task<string> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request);

    [Activity]
    Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request);

    [Activity]
    Task<string> SignTransactionAsync(SolanaSignTransactionRequest request);
}
