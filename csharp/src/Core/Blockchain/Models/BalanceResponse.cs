namespace Train.Solver.Core.Blockchain.Models;

public class BalanceResponse
{
    public string AmountInWei { get; set; }

    public decimal Amount { get; set; }

    public int Decimals { get; set; }
}
