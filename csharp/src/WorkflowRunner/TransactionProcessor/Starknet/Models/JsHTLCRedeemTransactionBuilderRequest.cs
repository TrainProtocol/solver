namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsHTLCRedeemTransactionBuilderRequest : JsHTLCTransferBuilderBase
{
    public string Id { get; set; } = null!;

    public string Secret { get; set; } = null!;
}
