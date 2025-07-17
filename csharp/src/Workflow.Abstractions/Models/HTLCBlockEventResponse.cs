namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCBlockEventResponse
{
    public List<HTLCCommitEventMessage> HTLCCommitEventMessages { get; set; } = new();
    public List<HTLCLockEventMessage> HTLCLockEventMessages { get; set; } = new();
}
