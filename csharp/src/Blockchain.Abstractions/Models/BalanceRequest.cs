using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class BalanceRequest : BaseRequest
{
    [ProtoMember(1)]
    public required string Address { get; set; }

    [ProtoMember(2)]
    public required string Asset { get; set; }
}
