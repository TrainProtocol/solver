namespace Train.Solver.Infrastructure.Abstractions.Models;

public class LimitDto
{
    public decimal MinAmountInUsd { get; set; }

    public decimal MinAmount { get; set; }

    public decimal MaxAmountInUsd { get; set; }

    public decimal MaxAmount { get; set; }
}