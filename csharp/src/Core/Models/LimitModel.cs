namespace Train.Solver.Core.Models;

public class LimitModel
{
    public RouteModel Route { get; set; } = null!;

    public decimal MinAmount { get; set; }

    public decimal MaxAmount { get; set; }
}
