using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;

public class NextNonceRequest : BaseRequest
{
    public required string Address { get; set; } = null!;
}
