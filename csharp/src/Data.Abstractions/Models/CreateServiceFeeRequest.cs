namespace Train.Solver.Data.Abstractions.Models;

public class CreateServiceFeeRequest
{
    public string Name { get; set; } 
    public decimal FeeInUsd { get; set; }
    public decimal PercentageFee { get; set; }
}
