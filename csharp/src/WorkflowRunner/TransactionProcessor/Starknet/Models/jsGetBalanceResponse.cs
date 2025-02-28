namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsGetBalanceResponse
{
    public string AmountInWei { get; set; }

    public decimal Amount { get; set; }

    public int Decimals { get; set; }
}
