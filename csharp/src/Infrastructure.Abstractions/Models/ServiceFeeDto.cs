namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ServiceFeeDto
{
    public decimal ServiceFeePercentage { get; set; }

    public string ServiceFeeInSource { get; set; } = null!;
}
