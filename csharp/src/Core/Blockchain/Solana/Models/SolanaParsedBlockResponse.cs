using System.Text.Json.Serialization;

namespace Train.Solver.Core.Blockchain.Solana.Models;

public class SolanaParsedBlockResponse
{
    [JsonPropertyName("result")]
    public BlockRequestResult Result { get; set; }
}

public class BlockRequestResult
{
    [JsonPropertyName("transactions")]
    public List<TransactionDetails> Transactions { get; set; }
}

public class TransactionDetails
{
    [JsonPropertyName("meta")]
    public TransactionMeta Meta { get; set; }

    [JsonPropertyName("transaction")]
    public ParsedTransactionDetails ParsedTransactionDetails { get; set; }
}
