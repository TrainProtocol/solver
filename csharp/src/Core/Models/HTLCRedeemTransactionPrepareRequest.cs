namespace Train.Solver.Core.Models;

public class HTLCRedeemTransactionPrepareRequest
{
    public string Id { get; set; }

    public string Secret { get; set; }

    public string Asset { get; set; }

    public string? DestinationAddress { get; set; }

    public string? SenderAddress { get; set; }
}
