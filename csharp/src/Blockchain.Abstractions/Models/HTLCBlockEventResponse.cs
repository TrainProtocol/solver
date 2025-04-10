using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCBlockEventResponse
{
    [ProtoMember(1)]
    public List<HTLCCommitEventMessage> HTLCCommitEventMessages { get; set; } = new();

    [ProtoMember(2)]
    public List<HTLCLockEventMessage> HTLCLockEventMessages { get; set; } = new();
}
