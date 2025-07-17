namespace Train.Solver.Workflow.Abstractions.Models;
public class EventRequest : BaseRequest
{
    public required ulong FromBlock { get; set; }

    public required ulong ToBlock { get; set; }

    public required string WalletAddress { get; set; }
}
