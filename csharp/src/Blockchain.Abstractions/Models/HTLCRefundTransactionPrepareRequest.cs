using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCRefundTransactionPrepareRequest
{
    [ProtoMember(1)]
    public string Id { get; set; } = null!;

    [ProtoMember(2)]
    public string Asset { get; set; } = null!;

    [ProtoMember(3)]
    public string? DestinationAddress { get; set; }
}
