namespace Train.Solver.AdminAPI.Models;

public class CreateServiceFeeRequest
{
    public decimal FeeInUsd { get; set; }
    public decimal PercentageFee { get; set; }
}