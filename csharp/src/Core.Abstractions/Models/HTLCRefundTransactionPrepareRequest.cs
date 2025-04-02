﻿namespace Train.Solver.Core.Abstractions.Models;

public class HTLCRefundTransactionPrepareRequest
{
    public string Id { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public string? DestinationAddress { get; set; }
}
