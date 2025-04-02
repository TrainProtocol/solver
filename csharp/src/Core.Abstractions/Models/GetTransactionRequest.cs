namespace Train.Solver.Core.Abstractions.Models;

public class GetTransactionRequest : BaseRequest
{
    public required string TransactionHash { get; set; } = null!;
}
