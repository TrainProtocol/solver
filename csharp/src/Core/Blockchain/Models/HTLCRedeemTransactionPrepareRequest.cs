using MessagePack;

namespace Train.Solver.Core.Blockchain.Models;

[MessagePackObject]
public class HTLCRedeemTransactionPrepareRequest 
{
    [Key(0)]
    public string Id { get; set; }

    [Key(1)]
    public string Secret { get; set; }

    [Key(2)]
    public string Asset { get; set; }

    [Key(3)]
    public string? DestinationAddress { get; set; }

    [Key(4)]
    public string? SenderAddress { get; set; }
}
