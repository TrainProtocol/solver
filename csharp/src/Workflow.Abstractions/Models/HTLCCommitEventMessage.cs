using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCCommitEventMessage
{
    public required string TxId { get; set; } = null!;

    public required string Id { get; set; } = null!;

    public required BigInteger Amount { get; set; }

    public required string ReceiverAddress { get; set; } = null!;

    public required string SourceNetwork { get; set; } = null!;

    public required string SenderAddress { get; set; } = null!;

    public required string SourceAsset { get; set; } = null!;

    public required string DestinationAddress { get; set; } = null!;

    public required string DestinationNetwork { get; set; } = null!;

    public required string DestinationAsset { get; set; } = null!;

    public required long TimeLock { get; set; }
}
