using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.Core.Abstractions.Models;

public class TransactionBuilderRequest : BaseRequest
{
    public TransactionType Type { get; set; }

    public string Args { get; set; } = null!;
}
