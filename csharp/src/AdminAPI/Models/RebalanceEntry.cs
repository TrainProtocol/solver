using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.AdminAPI.Models;

public class RebalanceEntry
{
    public required RebalanceSummary Summary { get; set; }

    public required string Id { get; set; }

    public required string Status { get; set; }

    public TransactionDto? Transaction { get; set; }
}
