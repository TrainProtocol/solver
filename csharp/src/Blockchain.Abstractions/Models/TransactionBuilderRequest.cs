using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

public class TransactionBuilderRequest : BaseRequest
{
    public TransactionType Type { get; set; }

    public string Args { get; set; } = null!;
}
