using System.Text.Json.Serialization;

namespace Train.Solver.API.Models;

public class QuoteModel
{
    [JsonPropertyName("total_fee")]
    public decimal TotalFee { get; set; }

    [JsonPropertyName("total_fee_in_usd")]
    public decimal TotalFeeInUsd { get; set; }

    [JsonPropertyName("receive_amount")]
    public decimal ReceiveAmount { get; set; }
}
