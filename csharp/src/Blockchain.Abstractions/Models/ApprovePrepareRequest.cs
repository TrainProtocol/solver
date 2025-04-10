using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class ApprovePrepareRequest
{
    [ProtoMember(1)]
    public required string Asset { get; set; } = null!;

    [ProtoMember(2)]
    public required decimal Amount { get; set; }
}
