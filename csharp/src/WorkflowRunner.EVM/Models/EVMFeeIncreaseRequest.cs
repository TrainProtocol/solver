using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.EVM.Models;

public class EVMFeeIncreaseRequest : BaseRequest
{
    public required Fee Fee { get; set; }
}
