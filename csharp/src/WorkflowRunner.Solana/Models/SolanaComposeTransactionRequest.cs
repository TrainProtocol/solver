using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.Solana.Models;

public class SolanaComposeTransactionRequest
{
    public required Fee Fee { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string CallData { get; set; } = null!;

    public required string LastValidBlockHash { get; set; } = null!;
}
