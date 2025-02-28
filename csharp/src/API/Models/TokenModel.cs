using System.Text.Json.Serialization;

namespace Train.Solver.API.Models;

public class TokenModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = null!;

    [JsonPropertyName("logo")]
    public string Logo { get; set; } = null!;

    [JsonPropertyName("contract")]
    public string? Contract { get; set; }

    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    [JsonPropertyName("precision")]
    public int Precision { get; set; }

    [JsonPropertyName("price_in_usd")]
    public decimal PriceInUsd { get; set; }

    [JsonPropertyName("listing_date")]
    public DateTimeOffset ListingDate { get; set; }
}