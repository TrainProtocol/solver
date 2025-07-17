using Train.Solver.Workflows.Abstractions.Models;

namespace Train.Solver.Workflows.EVM.Models;

public class EVMFeeIncreaseRequest : BaseRequest
{
    public required Fee Fee { get; set; }
}
