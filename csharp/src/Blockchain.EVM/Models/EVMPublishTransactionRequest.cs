using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.EVM.Models;
public class EVMPublishTransactionRequest: BaseRequest
{
    public string FromAddress { get; set; } = null!;

    public SignedTransaction SignedTransaction { get; set; } = null!;
}
