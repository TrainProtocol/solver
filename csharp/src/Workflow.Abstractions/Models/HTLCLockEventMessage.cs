namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCLockEventMessage
{
    public string TxId { get; set; } = null!;

    public string CommitId { get; set; } = null!;

    public string HashLock { get; set; } = null!;

    public long TimeLock { get; set; }
}
