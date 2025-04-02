namespace Train.Solver.Blockchain.Abstractions.Models;

public class BalanceRequest : BaseRequest
{
    public required string Address { get; set; }

    public required string Asset { get; set; }
}
