using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Models;

public class ApprovePrepareRequest 
{
    public required string Asset { get; set; } = null!;

    public required BigInteger Amount { get; set; }
}
