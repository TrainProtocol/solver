using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Models;

public class TransactionBuilderRequest : BaseRequest
{
    public TransactionType Type { get; set; }

    public string Args { get; set; } = null!;
}
