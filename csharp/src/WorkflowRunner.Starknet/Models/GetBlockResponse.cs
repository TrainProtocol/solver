using Newtonsoft.Json;

namespace Train.Solver.WorkflowRunner.Starknet.Models;

public class GetBlockResponse
{
    [JsonProperty("block_hash")]
    public string BlockHash { get; set; }

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }
}

public class GetBlockRequest
{
    [JsonProperty("block_number")]
    public long BlockNumber { get; set; }
}
