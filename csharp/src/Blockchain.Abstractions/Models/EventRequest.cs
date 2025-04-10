using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class EventRequest : BaseRequest
{
    [ProtoMember(1)]
    public required ulong FromBlock { get; set; }

    [ProtoMember(2)]
    public required ulong ToBlock { get; set; }
}
