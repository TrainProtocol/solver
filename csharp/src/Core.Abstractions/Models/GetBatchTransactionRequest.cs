namespace Train.Solver.Core.Abstractions.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionHashes { get; set; } = null!;
}
