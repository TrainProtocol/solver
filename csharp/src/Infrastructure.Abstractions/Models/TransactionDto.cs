using Train.Solver.Util.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class TransactionDto
{
    public TransactionType Type { get; set; }

    public string Hash { get; set; } = null!;

    public string Network { get; set; } = null!;
}
