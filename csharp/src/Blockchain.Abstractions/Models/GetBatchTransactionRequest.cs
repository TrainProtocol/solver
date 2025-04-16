namespace Train.Solver.Blockchain.Abstractions.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionHashes { get; set; } = null!;
}
