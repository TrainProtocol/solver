namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsApproveTransactionBuilderRequest : JsHTLCTransferBuilderBase
{
    public string Spender { get; set; } = null!;

    public string TokenContract { get; set; } = null!;

    public string AmountInWei { get; set; } = null!;
}
