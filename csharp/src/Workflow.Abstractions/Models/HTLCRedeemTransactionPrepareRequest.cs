namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCRedeemTransactionPrepareRequest
{
    public string CommitId { get; set; } = null!;

    public string Secret { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public string? DestinationAddress { get; set; }

    public string? SenderAddress { get; set; }
}
