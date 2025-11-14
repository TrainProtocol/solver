using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Models;

public class EstimateFeeRequest : BaseRequest
{
    public required string Asset { get; set; } = null!;

    public required string FromAddress { get; set; } = null!;

    public required string ToAddress { get; set; } = null!;

    public required BigInteger Amount { get; set; }

    public string? CallData { get; set; }
}
