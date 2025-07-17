using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;

public class EVMFeeIncreaseRequest : BaseRequest
{
    public required Fee Fee { get; set; }
}
