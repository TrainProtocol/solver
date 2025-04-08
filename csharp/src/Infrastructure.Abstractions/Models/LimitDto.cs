namespace Train.Solver.API.Models;

public class LimitDto
{
    public decimal MinAmountInUsd { get; set; }

    public decimal MinAmount { get; set; }

    public decimal MaxAmountInUsd { get; set; }

    public decimal MaxAmount { get; set; }
}