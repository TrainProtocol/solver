namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsGetAllowanceRequest
{
    public string NodeUrl { get; set; }

    public string TokenContract { get; set; }

    public string OwnerAddress { get; set; }

    public string SpenderAddress { get; set; }

    public int Decimals { get; set; }
}
