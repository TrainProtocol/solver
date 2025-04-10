using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class AllowanceRequest : BaseRequest
{
    [ProtoMember(1)]
    public string OwnerAddress { get; set; } = null!;

    [ProtoMember(2)]
    public string Asset { get; set; } = null!;
}
