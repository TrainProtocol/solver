namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsGetFeesRequest
{
    public string? CorrelationId { get; set; }

    public int Decimals { get; set; }

    public string FromAddress { get; set; }

    public string NodeUrl { get; set; }

    public string Symbol { get; set; }

    public string? TokenContract { get; set; }

    public string? CallData { get; set; }
}
