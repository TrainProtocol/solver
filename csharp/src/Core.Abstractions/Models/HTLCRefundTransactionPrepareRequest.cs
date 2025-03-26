namespace Train.Solver.Core.Abstractions.Models;

public class HTLCRefundTransactionPrepareRequest
{
    public string Id { get; set; }

    public string Asset { get; set; }

    public string? DestinationAddress { get; set; }
}
