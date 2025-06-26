using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ServiceFeeDto
{
    public decimal ServiceFeePercentage { get; set; }

    public BigInteger ServiceFee { get; set; }
}
