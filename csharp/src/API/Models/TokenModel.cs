namespace Train.Solver.API.Models;

public class TokenModel
{
    public string Symbol { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public string? Contract { get; set; }

    public int Decimals { get; set; }

    public int Precision { get; set; }

    public decimal PriceInUsd { get; set; }

    public DateTimeOffset ListingDate { get; set; }
}