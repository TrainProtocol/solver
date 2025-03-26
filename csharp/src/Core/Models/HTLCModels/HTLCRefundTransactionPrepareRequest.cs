namespace Train.Solver.Core.Models.HTLCModels;

public class HTLCRefundTransactionPrepareRequest
{
    public string Id { get; set; }

    public string Asset { get; set; }

    public string? DestinationAddress { get; set; }
}
