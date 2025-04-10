using ProtoBuf;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCCommitEventMessage
{
    [ProtoMember(1)]
    public required string TxId { get; set; } = null!;

    [ProtoMember(2)]
    public required string Id { get; set; } = null!;

    [ProtoMember(3)]
    public required decimal Amount { get; set; }

    [ProtoMember(4)]
    public required string AmountInWei { get; set; } = null!;

    [ProtoMember(5)]
    public required string ReceiverAddress { get; set; } = null!;

    [ProtoMember(6)]
    public required string SourceNetwork { get; set; } = null!;

    [ProtoMember(7)]
    public required string SenderAddress { get; set; } = null!;

    [ProtoMember(8)]
    public required string SourceAsset { get; set; } = null!;

    [ProtoMember(9)]
    public required string DestinationAddress { get; set; } = null!;

    [ProtoMember(10)]
    public required string DestinationNetwork { get; set; } = null!;

    [ProtoMember(11)]
    public required string DestinationAsset { get; set; } = null!;

    [ProtoMember(12)]
    public required long TimeLock { get; set; }

    [ProtoMember(13)]
    public required NetworkType DestinationNetworkType { get; set; }

    [ProtoMember(14)]
    public required NetworkType SourceNetworkType { get; set; }
}
