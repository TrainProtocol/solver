using System.Text.Json.Serialization;

namespace Train.Solver.Core.Blockchain.Models;

public class TransactionReceiptModel
{
    public string TransactionId { get; set; } = null!;

    public int Confirmations { get; set; }

    public long? BlockNumber { get; set; }

    public long Timestamp { get; set; }

    public string FeeAmountInWei { get; set; } = null!;

    public decimal FeeAmount { get; set; }

    public string FeeAsset { get; set; } = null!;

    public int FeeDecimals { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<TransactionStatuses>))]
    public TransactionStatuses Status { get; set; }

    public string? Nonce { get; set; }
}