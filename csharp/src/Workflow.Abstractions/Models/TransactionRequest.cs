using Train.Solver.Common.Enums;

namespace Train.Solver.Workflow.Abstractions.Models;

public class TransactionRequest : BaseRequest
{
    public required string PrepareArgs { get; set; } = null!;

    public required TransactionType Type { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string SignerAgentUrl  { get; set; }

    public int? SwapId { get; set; }
}
