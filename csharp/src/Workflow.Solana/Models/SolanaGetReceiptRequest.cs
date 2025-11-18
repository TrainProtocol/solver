using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflow.Solana.Models;

public class SolanaGetReceiptRequest
{
    public required DetailedNetworkDto Network { get; set; } = null!;

    public required string TxHash { get; set; } = null!;

    public required string TransactionBlockHeight { get; set; } = null!;
}
