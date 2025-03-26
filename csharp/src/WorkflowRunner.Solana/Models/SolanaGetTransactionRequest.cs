using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.Solana.Models;

public class SolanaGetTransactionRequest : GetTransactionRequest
{
    public required string FromAddress { get; set; } = null!;
}
