namespace Train.Solver.Core.Abstractions.Models;

public class GetBatchTransactionRequest : BaseRequest
{
    public string[] TransactionIds { get; set; } = null!;
}
