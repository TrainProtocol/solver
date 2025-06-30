using System.Text.Json.Serialization;

namespace Train.Solver.Infrastructure.Treasury.Client.Models;

public class SignRequestModel
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = null!;

    [JsonPropertyName("unsignedTxn")]
    public string UnsignedTxn { get; set; } = null!;
}