using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class AddLockSigTransactionPrepareRequest
{
    [ProtoMember(1)]
    public string Id { get; set; } = null!;

    [ProtoMember(2)]
    public string Hashlock { get; set; } = null!;

    [ProtoMember(3)]
    public long Timelock { get; set; }

    [ProtoMember(4)]
    public string? R { get; set; }

    [ProtoMember(5)]
    public string? S { get; set; }

    [ProtoMember(6)]
    public string? V { get; set; }

    [ProtoMember(7)]
    public string? Signature { get; set; }

    [ProtoMember(8)]
    public string Asset { get; set; } = null!;

    [ProtoMember(9)]
    public string[]? SignatureArray { get; set; }
}
