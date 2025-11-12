using System.Numerics;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;

public class EstimateFeeRequest : BaseRequest
{
    public required string Asset { get; set; } = null!;

    public required string FromAddress { get; set; } = null!;

    public required string ToAddress { get; set; } = null!;

    public required BigInteger Amount { get; set; }

    public string? CallData { get; set; }
}
