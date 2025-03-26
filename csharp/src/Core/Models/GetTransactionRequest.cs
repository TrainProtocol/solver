namespace Train.Solver.Core.Models;

public class GetTransactionRequest : BaseRequest
{
    public required string TransactionId { get; set; } = null!;
}
