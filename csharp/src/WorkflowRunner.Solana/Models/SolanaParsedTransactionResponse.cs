using System.Text.Json.Serialization;

namespace Train.Solver.Blockchains.Solana.Models;

public class SolanaParsedTransactionResponse
{
    [JsonPropertyName("result")]
    public ParsedTransaction Result { get; set; }
}

public class ParsedTransaction
{
    [JsonPropertyName("slot")]
    public long Slot { get; set; }

    [JsonPropertyName("transaction")]
    public ParsedTransactionDetails Transaction { get; set; }

    [JsonPropertyName("blockTime")]
    public long BlockTime { get; set; }

    [JsonPropertyName("meta")]
    public TransactionMeta Meta { get; set; }
}

public class ParsedTransactionDetails
{
    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("signatures")]
    public List<string> Signatures { get; set; }
}

public class Message
{
    [JsonPropertyName("instructions")]
    public List<Instruction> Instructions { get; set; }
}

public class TransactionMeta
{
    [JsonPropertyName("err")]
    public object Err { get; set; } 

    [JsonPropertyName("fee")]
    public long Fee { get; set; }

    [JsonPropertyName("postTokenBalances")]
    public List<TokenBalance> PostTokenBalances { get; set; }
}

public class TokenBalance
{   
    [JsonPropertyName("mint")]
    public string Mint { get; set; }

    [JsonPropertyName("owner")]
    public string Owner { get; set; }
}

public class Instruction
{
    [JsonPropertyName("parsed")]
    public ParsedInstructionData Parsed { get; set; }

    [JsonPropertyName("programId")]
    public string ProgramId { get; set; }
}


public class ParsedInstructionData
{
    [JsonPropertyName("info")]
    public TransferInfo Info { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    public string Memo { get; set; }
}

public class TransferInfo
{

    [JsonPropertyName("amount")]
    public string Amount { get; set; }

    [JsonPropertyName("destination")]
    public string Destination { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("lamports")]
    public ulong? Lamports { get; set; }

    [JsonPropertyName("tokenAmount")]
    public UiTokenAmount? tokenAmount { get; set; }
}

public class UiTokenAmount
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; }
}
