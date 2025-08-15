using System.Numerics;
using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class UpdateRouteRequest
{
    public string RateProvider { get; set; } = default!;
    public BigInteger MinAmount { get; set; }
    public BigInteger MaxAmount { get; set; }
    public string ServiceFee { get; set; } = null!;
    public RouteStatus Status { get; set; }
    public bool IgnoreExpenseFee { get; set; }
}