using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.WorkflowRunner.EVM.Models;

public class EVMFeeIncreaseRequest : BaseRequest
{
    public required Fee Fee { get; set; }
}
