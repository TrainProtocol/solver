namespace Train.Solver.Infrastructure.Abstractions.Models;

public class TokenDto
{
    public string Symbol { get; set; } = null!;

    public string? Contract { get; set; }

    public int Decimals { get; set; }
}