using System.Numerics;

namespace Train.Solver.Workflow.EVM.Models;

public class ApprovePrepareRequest 
{
    public required string Asset { get; set; } = null!;

    public required BigInteger Amount { get; set; }
}
