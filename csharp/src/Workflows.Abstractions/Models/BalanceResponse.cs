namespace Train.Solver.Workflows.Abstractions.Models;

public class BalanceResponse
{
    public string AmountInWei { get; set; } = null!;

    //public decimal Amount { get; set; }

    public int Decimals { get; set; }
}
