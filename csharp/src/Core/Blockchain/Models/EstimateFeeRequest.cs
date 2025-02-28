namespace Train.Solver.Core.Blockchain.Models;

public class EstimateFeeRequest
{
    public string Asset { get; set; } = null!;

    public string FromAddress { get; set; } = null!;

    public string ToAddress { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? CallData { get; set; }
}
