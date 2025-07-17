namespace Train.Solver.Workflows.Abstractions.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionHashes { get; set; } = null!;
}
