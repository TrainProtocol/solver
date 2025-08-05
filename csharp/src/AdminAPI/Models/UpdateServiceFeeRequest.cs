namespace Train.Solver.AdminAPI.Models;

public class UpdateServiceFeeRequest
{
    public decimal FeeInUsd { get; set; }
    public decimal PercentageFee { get; set; }
}