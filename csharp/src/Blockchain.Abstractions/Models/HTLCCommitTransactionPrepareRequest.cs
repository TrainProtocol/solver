using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCCommitTransactionPrepareRequest
{
    [ProtoMember(1)]
    public string Receiever { get; set; } = null!;

    [ProtoMember(2)]
    public string[] HopChains { get; set; }

    [ProtoMember(3)]
    public string[] HopAssets { get; set; }

    [ProtoMember(4)]
    public string[] HopAddresses { get; set; }

    [ProtoMember(5)]
    public string DestinationChain { get; set; } = null!;

    [ProtoMember(6)]
    public string DestinationAsset { get; set; } = null!;

    [ProtoMember(7)]
    public string DestinationAddress { get; set; } = null!;

    [ProtoMember(8)]
    public string SourceAsset { get; set; } = null!;

    [ProtoMember(9)]
    public long Timelock { get; set; }

    [ProtoMember(10)]
    public decimal Amount { get; set; }
}
