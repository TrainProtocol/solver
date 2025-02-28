using Train.Solver.Data.Entities;

namespace Train.Solver.API.Models;

public class TransactionModel
{
    public TransactionType Type { get; set; }

    public string Hash { get; set; } = null!;

    public string Network { get; set; } = null!;
}
