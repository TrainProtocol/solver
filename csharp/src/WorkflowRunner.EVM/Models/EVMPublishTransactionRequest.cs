using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.WorkflowRunner.EVM.Models;
public class EVMPublishTransactionRequest: BaseRequest
{
    public string FromAddress { get; set; } = null!;

    public SignedTransaction SignedTransaction { get; set; } = null!;
}
