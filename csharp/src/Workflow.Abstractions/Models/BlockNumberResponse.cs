namespace Train.Solver.Workflow.Abstractions.Models;

public class BlockNumberResponse
{
    public ulong BlockNumber { get; set; }

    public string? BlockHash { get; set; }
}
