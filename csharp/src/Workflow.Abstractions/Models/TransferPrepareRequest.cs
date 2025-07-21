using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Models;

public class TransferPrepareRequest 
{
    public string ToAddress { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public BigInteger Amount { get; set; }

    public string? Memo { get; set; }

    public string? FromAddress { get; set; }
}
