using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.Blockchains.EVM.Models;

public class EVMFeeIncreaseRequest : BaseRequest
{
    public required Fee Fee { get; set; }
}
