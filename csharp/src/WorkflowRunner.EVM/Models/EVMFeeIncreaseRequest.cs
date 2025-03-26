using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.EVM.Models;

public class EVMFeeIncreaseRequest : BaseRequest
{
    public required Fee Fee { get; set; }
}
