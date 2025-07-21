using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class LimitDto
{
    public required string MinAmount { get; set; }

    public required string MaxAmount { get; set; }
}