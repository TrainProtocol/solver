namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCRefundTransactionPrepareRequest
{
    public string CommitId { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public string? DestinationAddress { get; set; }
}
