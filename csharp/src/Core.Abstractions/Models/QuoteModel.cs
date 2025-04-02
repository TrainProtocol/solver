namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteModel
{
    public RouteModel Route { get; set; } = null!;

    public decimal ReceiveAmount { get; set; }

    public decimal TotalFee { get; set; }
}
