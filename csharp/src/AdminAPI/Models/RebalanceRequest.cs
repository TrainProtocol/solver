using System.Numerics;

namespace Train.Solver.AdminAPI.Models;

public class RebalanceRequest
{
    public string NetworkName { get; set; } = null!;

    public string Token { get; set; } = null!;

    public BigInteger Amount { get; set; }

    public string FromAddress { get; set; } = null!;

    public string ToAddress { get; set; } = null!;
}
