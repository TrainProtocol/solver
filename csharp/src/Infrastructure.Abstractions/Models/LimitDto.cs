namespace Train.Solver.Infrastructure.Abstractions.Models;

public class LimitDto
{
    public decimal MinAmountInUsd { get; set; }

    public string MinAmount { get; set; } = null!;

    public decimal MaxAmountInUsd { get; set; }

    public string MaxAmount { get; set; } = null!;
}