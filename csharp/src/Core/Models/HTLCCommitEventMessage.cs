﻿using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Models;

public class HTLCCommitEventMessage
{
    public required string TxId { get; set; } = null!;

    public required string Id { get; set; } = null!;

    public required decimal Amount { get; set; }

    public required string AmountInWei { get; set; } = null!;

    public required string ReceiverAddress { get; set; } = null!;

    public required string SourceNetwork { get; set; } = null!;

    public required string SenderAddress { get; set; } = null!;

    public required string SourceAsset { get; set; } = null!;

    public required string DestinationAddress { get; set; } = null!;

    public required string DestinationNetwork { get; set; } = null!;

    public required string DestinationAsset { get; set; } = null!;

    public required long TimeLock { get; set; }

    public required NetworkGroup DestinationNetwrokGroup { get; set; }

    public required NetworkGroup SourceNetwrokGroup { get; set; }
}
