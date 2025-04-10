using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class BlockNumberResponse
{
    [ProtoMember(1)]
    public ulong BlockNumber { get; set; }

    [ProtoMember(2)]
    public string? BlockHash { get; set; }
}
