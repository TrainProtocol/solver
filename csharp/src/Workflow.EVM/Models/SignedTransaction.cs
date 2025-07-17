namespace Train.Solver.Workflow.EVM.Models;

public class SignedTransaction
{
    public string Hash { get; set; } = null!;

    public string RawTxn { get; set; } = null!;
}
