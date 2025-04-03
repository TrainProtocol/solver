namespace Train.Solver.Blockchain.Starknet.Models;

public class GetEventsResponse
{
    [Newtonsoft.Json.JsonProperty("events")]
    public EventData[] Events { get; set; }

    public class EventData
    {
        [Newtonsoft.Json.JsonProperty("transaction_hash")]
        public string TransactionHash { get; set; } = null!;

        [Newtonsoft.Json.JsonProperty("data")]
        public List<string> Data { get; set; } = new List<string>();

        [Newtonsoft.Json.JsonProperty("keys")]
        public List<string> Keys { get; set; } = new List<string>();

        [Newtonsoft.Json.JsonProperty("block_number")]
        public int? BlockNumber { get; set; }
    }
}