namespace Train.Solver.Workflow.Abstractions.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionHashes { get; set; } = null!;
}
