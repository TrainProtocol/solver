using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class SpenderAddressRequest : BaseRequest
{
    [ProtoMember(1)]
    public required string Asset { get; set; }
}
