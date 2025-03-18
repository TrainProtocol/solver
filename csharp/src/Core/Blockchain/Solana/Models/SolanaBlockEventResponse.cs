using System.Text.Json.Serialization;

namespace Train.Solver.Core.Blockchain.Solana.Models;

public class SolanaBlockEventResponse
{
    [JsonPropertyName("result")]
    public BlockEventRequestResult Result { get; set; }
}

public class BlockEventRequestResult
{
    [JsonPropertyName("transactions")]
    public List<TransactionEventDetails> Transactions { get; set; }
}

public class TransactionEventDetails
{
    [JsonPropertyName("meta")]
    public EventTransactionMeta Meta { get; set; }

    [JsonPropertyName("transaction")]
    public ParsedEventTransactionDetails Transaction { get; set; }
}

public class EventTransactionMeta
{
    [JsonPropertyName("logMessages")]
    public List<string> LogMessages { get; set; }
}

public class ParsedEventTransactionDetails
{
    [JsonPropertyName("message")]
    public EventMessageInstructions Message { get; set; }

    [JsonPropertyName("signatures")]
    public List<string> Signatures { get; set; }
}

public class EventMessageInstructions
{
    [JsonPropertyName("instructions")]
    public List<EventInstruction> Instructions { get; set; }
}

public class EventInstruction
{
    [JsonPropertyName("programId")]
    public string ProgramId { get; set; }
}
