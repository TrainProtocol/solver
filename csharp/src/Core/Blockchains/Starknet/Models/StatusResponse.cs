namespace Train.Solver.Core.Blockchains.Starknet.Models;

public class StatusResponse
{
    [Newtonsoft.Json.JsonProperty("finality_status")]
    public string FinalityStatus { get; set; } = null!;

    [Newtonsoft.Json.JsonProperty("execution_status")]
    public string ExecutionStatus { get; set; } = null!;
}
