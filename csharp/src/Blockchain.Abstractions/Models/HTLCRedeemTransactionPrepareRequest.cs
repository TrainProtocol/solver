namespace Train.Solver.Blockchain.Abstractions.Models;

public class HTLCRedeemTransactionPrepareRequest
{
    public string Id { get; set; } = null!;

    public string Secret { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public string? DestinationAddress { get; set; }

    public string? SenderAddress { get; set; }
}
