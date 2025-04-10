using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class HTLCLockEventMessage
{
    [ProtoMember(1)]
    public string TxId { get; set; } = null!;

    [ProtoMember(2)]
    public string Id { get; set; } = null!;

    [ProtoMember(3)]
    public string HashLock { get; set; } = null!;

    [ProtoMember(4)]
    public long TimeLock { get; set; }
}
