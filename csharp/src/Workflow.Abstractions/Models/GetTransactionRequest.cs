namespace Train.Solver.Workflow.Abstractions.Models;

public class GetTransactionRequest : BaseRequest
{
    public required string TransactionHash { get; set; }
}
