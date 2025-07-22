using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ServiceFeeDto
{
    public string Name { get; set; }

    public decimal Percentage { get; set; }

    public decimal UsdAmount { get; set; }
}
