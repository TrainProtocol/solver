namespace Train.Solver.AdminAPI.Models;

public class RebalanceRequest
{
    public string NetworkName { get; set; } = null!;

    public string Token { get; set; } = null!;

    public decimal Amount { get; set; }

    public string To { get; set; } = null!;
}
