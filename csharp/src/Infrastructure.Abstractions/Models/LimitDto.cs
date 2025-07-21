using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class LimitDto
{
    //public decimal MinAmountInUsd { get; set; }

    public BigInteger MinAmount { get; set; }

    //public decimal MaxAmountInUsd { get; set; }

    public BigInteger MaxAmount { get; set; }
}