using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;
public class EVMPublishTransactionRequest: BaseRequest
{
    public string FromAddress { get; set; } = null!;

    public SignedTransaction SignedTransaction { get; set; } = null!;
}
