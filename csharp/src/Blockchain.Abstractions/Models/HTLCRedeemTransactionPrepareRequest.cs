using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCRedeemTransactionPrepareRequest
{
    [ProtoMember(1)]
    public string Id { get; set; } = null!;

    [ProtoMember(2)]
    public string Secret { get; set; } = null!;

    [ProtoMember(3)]
    public string Asset { get; set; } = null!;

    [ProtoMember(4)]
    public string? DestinationAddress { get; set; }

    [ProtoMember(5)]
    public string? SenderAddress { get; set; }
}
