using System.Text.Json.Serialization;

namespace Train.Solver.Infrastrucutre.Secret.Treasury.Models;

public class SignTransactionResponseModel
{
    [JsonPropertyName("signedTxn")]
    public string SignedTxn { get; set; } = null!;
}
