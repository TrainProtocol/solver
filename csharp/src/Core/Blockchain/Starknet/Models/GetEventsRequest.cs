namespace Train.Solver.Core.Blockchain.Starknet.Models;

public class GetEventsRequest<TBlock> : GetEventsRequestBase where TBlock : class
{
    [Newtonsoft.Json.JsonProperty("to_block")]
    public TBlock ToBlock { get; set; } = null!;
}

public abstract class GetEventsRequestBase
{
    [Newtonsoft.Json.JsonProperty("from_block")]
    public Block FromBlock { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("address")]
    public string Address { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("chunk_size")]
    public int ChunkSize { get; set; } = 1000;
}

public class Block
{
    [Newtonsoft.Json.JsonProperty("block_number")]
    public int BlockNumber { get; set; }
}
