using System.Text.Json.Serialization;

namespace Train.Solver.Core.Blockchains.Solana.Models;

public class SolanaAccountInfoResponse
{
    [JsonPropertyName("result")]
    public AccountInfoResult Result { get; set; }
}

public class AccountInfoResult
{
    [JsonPropertyName("value")]
    public AccountValue Value { get; set; }
}

public class AccountValue
{
    [JsonPropertyName("data")]
    public ParsedData Data { get; set; }

}

public class ParsedData
{
    [JsonPropertyName("parsed")]
    public ParsedInfo Parsed { get; set; }

}

public class ParsedInfo
{
    [JsonPropertyName("info")]
    public TokenAccountInfo Info { get; set; }
}

public class TokenAccountInfo
{
    [JsonPropertyName("mint")]
    public string Mint { get; set; }

    [JsonPropertyName("owner")]
    public string Owner { get; set; }
}
