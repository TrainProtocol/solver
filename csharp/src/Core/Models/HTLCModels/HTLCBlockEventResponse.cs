namespace Train.Solver.Core.Models.HTLCModels;

public class HTLCBlockEventResponse
{
    public List<HTLCCommitEventMessage> HTLCCommitEventMessages { get; set; } = new();
    public List<HTLCLockEventMessage> HTLCLockEventMessages { get; set; } = new();
}
