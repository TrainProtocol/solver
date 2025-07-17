namespace Train.Solver.Workflow.Abstractions.Models;

public class NextNonceRequest : BaseRequest
{
    public required string Address { get; set; } = null!;
}
