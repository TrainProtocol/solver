using Train.Solver.Common.Enums;

namespace Train.Solver.Workflow.Abstractions.Models;

public class TransactionBuilderRequest : BaseRequest
{
    public TransactionType Type { get; set; }

    public string Args { get; set; } = null!;
}
