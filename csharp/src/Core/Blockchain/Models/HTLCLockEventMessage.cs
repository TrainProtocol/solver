namespace Train.Solver.Core.Blockchain.Models;

public class HTLCLockEventMessage
{
    public string TxId { get; set; } = null!;

    public string Id { get; set; } = null!;

    public string HashLock { get; set; } = null!;

    public long TimeLock { get; set; }
}
