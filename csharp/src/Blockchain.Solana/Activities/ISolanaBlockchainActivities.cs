using Temporalio.Activities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Solana.Models;

namespace Train.Solver.Blockchain.Solana.Activities;

public interface ISolanaBlockchainActivities
{
    [Activity]
    Task<Fee> EstimateFeeAsync(EstimateFeeRequest request);

    [Activity]
    Task<string> GetNextNonceAsync(NextNonceRequest request);

    [Activity]
    Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request);

    [Activity]
    Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request);

    [Activity]
    Task SimulateTransactionAsync(SolanaPublishTransactionRequest request);

    [Activity]
    Task<byte[]> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request);

    [Activity]
    Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request);
}
