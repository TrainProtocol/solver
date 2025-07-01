using System.Text.Json.Serialization;

namespace Train.Solver.Infrastructure.Treasury.Client.Models;

public class SignTransactionResponseModel
{
    [JsonPropertyName("signedTxn")]
    public string SignedTxn { get; set; } = null!;
}
