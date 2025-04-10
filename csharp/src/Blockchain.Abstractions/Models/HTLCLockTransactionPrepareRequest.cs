using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCLockTransactionPrepareRequest
{
    [ProtoMember(1)]
    public string Receiver { get; set; } = null!;

    [ProtoMember(2)]
    public string Hashlock { get; set; } = null!;

    [ProtoMember(3)]
    public long Timelock { get; set; }

    [ProtoMember(4)]
    public string SourceAsset { get; set; } = null!;

    [ProtoMember(5)]
    public string SourceNetwork { get; set; } = null!;

    [ProtoMember(6)]
    public string DestinationNetwork { get; set; } = null!;

    [ProtoMember(7)]
    public string DestinationAddress { get; set; } = null!;

    [ProtoMember(8)]
    public string DestinationAsset { get; set; } = null!;

    [ProtoMember(9)]
    public string Id { get; set; } = null!;

    [ProtoMember(10)]
    public decimal Amount { get; set; }

    [ProtoMember(11)]
    public decimal Reward { get; set; }

    [ProtoMember(12)]
    public long RewardTimelock { get; set; }
}
