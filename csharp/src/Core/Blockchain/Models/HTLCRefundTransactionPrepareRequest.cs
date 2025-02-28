using MessagePack;

namespace Train.Solver.Core.Blockchain.Models;


[MessagePackObject]
public class HTLCRefundTransactionPrepareRequest 
{
    [Key(0)]
    public string Id { get; set; }

    [Key(1)]
    public string Asset { get; set; }

    [Key(2)]
    public string? DestinationAddress { get; set; }
}
