using MessagePack;

namespace Train.Solver.Core.Models;


[MessagePackObject]
public class HTLCCommitTransactionPrepareRequest
{
    [Key(0)]
    public string Receiever { get; set; } = null!;

    [Key(1)]
    public string[] HopChains { get; set; }

    [Key(2)]
    public string[] HopAssets { get; set; }

    [Key(3)]
    public string[] HopAddresses { get; set; }

    [Key(4)]
    public string DestinationChain { get; set; }

    [Key(5)]
    public string DestinationAsset { get; set; }

    [Key(6)]
    public string DestinationAddress { get; set; }

    [Key(7)]
    public string SourceAsset { get; set; } = null!;

    [Key(8)]
    public long Timelock { get; set; }

    [Key(9)]
    public decimal Amount { get; set; }

}
