using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class TransferPrepareRequest
{
    [ProtoMember(1)]
    public string ToAddress { get; set; } = null!;

    [ProtoMember(2)]
    public string Asset { get; set; } = null!;

    [ProtoMember(3)]
    public decimal Amount { get; set; }

    [ProtoMember(4)]
    public string? Memo { get; set; }

    [ProtoMember(5)]
    public string? FromAddress { get; set; }
}
