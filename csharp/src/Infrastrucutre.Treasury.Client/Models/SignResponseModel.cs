using System.Text.Json.Serialization;

namespace Train.Solver.Infrastructure.Treasury.Client.Models;

public class SignResponseModel
{
    [JsonPropertyName("signedTxn")]
    public string SignedTxn { get; set; } = null!;
}
