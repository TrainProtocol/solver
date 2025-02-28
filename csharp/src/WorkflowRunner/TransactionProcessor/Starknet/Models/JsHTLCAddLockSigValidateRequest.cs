namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsHTLCAddLockSigValidateRequest : JsHTLCTransferBuilderBase
{
    public string Id { get; set; }

    public string Hashlock { get; set; }

    public string Timelock { get; set; }

    public string[] SignatureArray { get; set; }

    public string ChainId { get; set; }

    public string NodeUrl { get; set; }

    public string SignerAddress { get; set; }
}
