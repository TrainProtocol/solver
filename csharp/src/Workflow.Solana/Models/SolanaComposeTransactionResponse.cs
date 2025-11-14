namespace Train.Solver.Workflow.Solana.Models;

public class SolanaComposeTransactionResponse
{
    public required string LastValidBlockHeight { get; set; } = null!;

    public required string RawTx { get; set; } = null!;
}
