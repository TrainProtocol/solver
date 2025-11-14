using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Solana.Models;

public class SolanaSignTransactionRequest : BaseRequest
{
    public required string SignerAgentUrl { get; set; }

    public required string UnsignRawTransaction { get; set; } = null!;

    public required string FromAddress { get; set; } = null!;
}
