namespace Train.Solver.Core.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionIds { get; set; } = null!;
}
