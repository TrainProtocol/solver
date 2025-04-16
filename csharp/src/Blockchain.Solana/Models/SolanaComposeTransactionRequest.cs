using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaComposeTransactionRequest
{
    public required Fee Fee { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string CallData { get; set; } = null!;

    public required string LastValidBlockHash { get; set; } = null!;
}
