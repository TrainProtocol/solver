using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.EVM.Models;
public class EVMPublishTransactionRequest: BaseRequest
{
    public string FromAddress { get; set; } = null!;

    public SignedTransaction SignedTransaction { get; set; } = null!;
}
