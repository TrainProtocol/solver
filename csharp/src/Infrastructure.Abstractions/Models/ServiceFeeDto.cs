using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ServiceFeeDto
{
    public decimal Percentage { get; set; }

    public decimal UsdAmount { get; set; }
}
