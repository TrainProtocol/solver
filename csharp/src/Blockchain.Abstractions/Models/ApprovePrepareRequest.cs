﻿namespace Train.Solver.Blockchain.Abstractions.Models;

public class ApprovePrepareRequest 
{
    public required string Asset { get; set; } = null!;

    public required decimal Amount { get; set; }
}
