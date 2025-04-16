using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaGetTransactionRequest : GetTransactionRequest
{
    public required string FromAddress { get; set; } = null!;
}
