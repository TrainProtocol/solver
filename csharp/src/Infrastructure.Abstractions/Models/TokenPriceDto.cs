namespace Train.Solver.Infrastructure.Abstractions.Models;

public class TokenPriceDto
{
    public string Symbol { get; set; } = null!;

    public decimal PriceInUsd { get; set; }

    public string ExternalId { get; set; } = null!;

    public DateTimeOffset LastUpdated { get; set; }
}