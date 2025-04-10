using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class NextNonceRequest : BaseRequest
{
    [ProtoMember(1)]
    public required string Address { get; set; } = null!;
}
