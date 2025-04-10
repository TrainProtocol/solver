using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class AddLockSignatureRequest : AddLockSignatureModel
{
    [ProtoMember(1)]
    public required string Id { get; set; } = null!;

    [ProtoMember(2)]
    public required string Hashlock { get; set; } = null!;

    [ProtoMember(3)]
    public required string SignerAddress { get; set; } = null!;

    [ProtoMember(4)]
    public required string Asset { get; set; } = null!;

    [ProtoMember(5)]
    public required string NetworkName { get; set; } = null!;
}

[ProtoContract]
[ProtoInclude(1000, typeof(AddLockSignatureRequest))]
public class AddLockSignatureModel
{
    [ProtoMember(1)]
    public string? R { get; set; }

    [ProtoMember(2)]
    public string? S { get; set; }

    [ProtoMember(3)]
    public string? V { get; set; }

    [ProtoMember(4)]
    public string? Signature { get; set; }

    [ProtoMember(5)]
    public string[]? SignatureArray { get; set; }

    [ProtoMember(6)]
    public required long Timelock { get; set; }
}
