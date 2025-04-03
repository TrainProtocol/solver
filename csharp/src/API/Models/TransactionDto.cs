using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.API.Models;

public class TransactionDto
{
    public TransactionType Type { get; set; }

    public string Hash { get; set; } = null!;

    public string Network { get; set; } = null!;
}
