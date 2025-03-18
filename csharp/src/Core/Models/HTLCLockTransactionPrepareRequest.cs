using MessagePack;

namespace Train.Solver.Core.Models;


[MessagePackObject]
public class HTLCLockTransactionPrepareRequest
{
    [Key(0)]
    public string Receiver { get; set; } = null!;

    [Key(1)]
    public string Hashlock { get; set; }

    [Key(2)]
    public long Timelock { get; set; }

    [Key(3)]
    public string SourceAsset { get; set; } = null!;

    [Key(4)]
    public string SourceNetwork { get; set; } = null!;

    [Key(5)]
    public string DestinationNetwork { get; set; } = null!;

    [Key(6)]
    public string DestinationAddress { get; set; } = null!;

    [Key(7)]
    public string DestinationAsset { get; set; } = null!;

    [Key(8)]
    public string Id { get; set; }

    [Key(9)]
    public decimal Amount { get; set; }

    [Key(10)]
    public decimal Reward { get; set; }

    [Key(11)]
    public long RewardTimelock { get; set; }
}
