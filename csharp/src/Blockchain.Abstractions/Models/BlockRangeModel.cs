using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class BlockRangeModel
{
    [ProtoMember(1)]
    public ulong From { get; set; }

    [ProtoMember(2)]
    public ulong To { get; set; }
}
