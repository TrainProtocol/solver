using Train.Solver.Common.Enums;

namespace Train.Solver.Workflows.Abstractions.Models;

public class TransactionBuilderRequest : BaseRequest
{
    public TransactionType Type { get; set; }

    public string Args { get; set; } = null!;
}
