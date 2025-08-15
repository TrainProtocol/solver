using Temporalio.Api.Enums.V1;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.AdminAPI.Models;

public class RebalanceEntry
{
    public required RebalanceSummary Summary { get; set; }

    public required string Id { get; set; }

    public required WorkflowExecutionStatus Status { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public TransactionDto? Transaction { get; set; }
}
