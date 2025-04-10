using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HashlockModel
{
    [ProtoMember(1)]
    public required string Secret { get; set; } = null!;

    [ProtoMember(2)]
    public required string Hash { get; set; } = null!;
}
