namespace Train.Solver.Core.Abstractions.Models;

public class SufficientBalanceRequest : BalanceRequest
{
    public required decimal Amount { get; set; }
}
