namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsSufficientBalanceRequest
{
    public string? Address { get; set; }

    public string? Symbol { get; set; }

    public string? CorrelationId { get; set; }

    public string? NodeUrl { get; set; }

    public string? TokenContract { get; set; }

    public int Decimals { get; set; }

    public decimal Amount { get; set; }
}
