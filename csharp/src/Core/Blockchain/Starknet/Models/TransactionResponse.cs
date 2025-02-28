namespace Train.Solver.Core.Blockchain.Starknet.Models;

public class TransactionResponse
{
    [Newtonsoft.Json.JsonProperty("transaction_hash")]
    public string TransactionHash { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("finality_status")]
    public string FinalityStatus { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("execution_status")]
    public string? ExecutionStatus { get; set; }

    [Newtonsoft.Json.JsonProperty("actual_fee")]
    public ActualFee ActualFee { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("block_number")]
    public long? BlockNumber { get; set; }

    [Newtonsoft.Json.JsonProperty("events")]
    public TransactionEvent[] Events { get; set; }
}

public class TransactionEvent
{
    [Newtonsoft.Json.JsonProperty("from_address")]
    public string FromAddress { get; set; }

    [Newtonsoft.Json.JsonProperty("data")]
    public List<string> Data { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("keys")]
    public List<string> Keys { get; set; }
}

public class ActualFee
{
    [Newtonsoft.Json.JsonProperty("amount")]
    public string Amount { get; set; }

    [Newtonsoft.Json.JsonProperty("unit")]
    public string Unit{ get; set; }
}