using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;

public class AllowanceRequest : BaseRequest
{
    public string OwnerAddress { get; set; } = null!;

    public string Asset { get; set; } = null!;
}
