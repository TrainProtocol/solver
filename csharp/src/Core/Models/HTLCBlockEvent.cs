namespace Train.Solver.Core.Models;

public class HTLCBlockEvent
{
    public List<HTLCCommitEventMessage> HTLCCommitEventMessages { get; set; } = new();
    public List<HTLCLockEventMessage> HTLCLockEventMessages { get; set; } = new();
}
