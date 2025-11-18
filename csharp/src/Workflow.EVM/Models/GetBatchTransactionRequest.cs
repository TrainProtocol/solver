using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionHashes { get; set; } = null!;
}
