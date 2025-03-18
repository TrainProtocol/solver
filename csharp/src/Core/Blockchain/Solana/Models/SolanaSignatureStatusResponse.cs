using System.Text.Json.Serialization;

namespace Train.Solver.Core.Blockchain.Solana.Models;

public class SolanaSignatureStatusResponse
{
    [JsonPropertyName("result")]
    public SignatureStatusResult Result { get; set; }
}

public class SignatureStatusResult
{
    [JsonPropertyName("value")]
    public List<SignatureStatus> Value { get; set; }
}

public class SignatureStatus
{
    [JsonPropertyName("slot")]
    public ulong Slot { get; set; }

    [JsonPropertyName("confirmations")]
    public int? Confirmations { get; set; }

    [JsonPropertyName("err")]
    public object Error { get; set; }

    [JsonPropertyName("confirmationStatus")]
    public string ConfirmationStatus { get; set; }
}
