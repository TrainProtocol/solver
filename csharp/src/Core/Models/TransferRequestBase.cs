namespace Train.Solver.Core.Models;
public abstract class TransferRequestBase
{
    public string ReferenceId { get; set; } = null!;

    public string FromAddress { get; set; } = null!;

    public string ToAddress { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public decimal Amount { get; set; }
}
