using System.Text.Json.Serialization;

namespace Train.Solver.API.Models;

public class LimitModel
{
    [JsonPropertyName("min_amount_in_usd")]
    public decimal MinAmountInUsd { get; set; }

    [JsonPropertyName("min_amount")]
    public decimal MinAmount { get; set; }

    [JsonPropertyName("max_amount_in_usd")]
    public decimal MaxAmountInUsd { get; set; }

    [JsonPropertyName("max_amount")]
    public decimal MaxAmount { get; set; }
}