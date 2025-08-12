using Train.Solver.Common.Enums;

namespace Train.Solver.Workflow.Abstractions.Models;

public class PrepareTransactionRequest
{
    public TransactionType Type { get; set; }

    public string PrepareArgs { get; set; } = null!;

    public string NetworkName { get; set; } = null!;
}
