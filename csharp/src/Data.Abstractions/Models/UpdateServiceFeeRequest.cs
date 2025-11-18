namespace Train.Solver.Data.Abstractions.Models;

public class UpdateServiceFeeRequest
{
    public decimal FeeInUsd { get; set; }
    public decimal PercentageFee { get; set; }
}