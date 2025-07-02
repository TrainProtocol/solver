using System.Text.Json.Serialization;

namespace Train.Solver.Infrastrucutre.Secret.Treasury.Models;

public class SignTransactionRequestModel
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = null!;

    [JsonPropertyName("unsignedTxn")]
    public string UnsignedTxn { get; set; } = null!;
}