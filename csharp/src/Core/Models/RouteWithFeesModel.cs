namespace Train.Solver.Core.Models;

public class RouteWithFeesModel : RouteModel
{
    public decimal ServiceFeeInSource { get; set; }

    public decimal ServiceFeePercentage { get; set; }

    public decimal ExpenseInSource { get; set; }
}
