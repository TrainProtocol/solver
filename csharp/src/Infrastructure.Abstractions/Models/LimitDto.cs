using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class LimitDto
{
    public BigInteger MinAmount { get; set; }

    public BigInteger MaxAmount { get; set; }
}