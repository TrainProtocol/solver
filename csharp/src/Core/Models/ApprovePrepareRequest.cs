﻿namespace Train.Solver.Core.Models;

public class ApprovePrepareRequest 
{
    public required string SpenderAddress { get; set; } = null!;

    public required string Asset { get; set; } = null!;

    public required decimal Amount { get; set; }
}
