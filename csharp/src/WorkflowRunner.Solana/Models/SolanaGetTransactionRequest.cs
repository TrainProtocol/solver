using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.WorkflowRunner.Solana.Models;

public class SolanaGetTransactionRequest : GetTransactionRequest
{
    public required string FromAddress { get; set; } = null!;
}
