﻿namespace Train.Solver.Core.Models;

public class EstimateFeeRequest
{
    public required string Asset { get; set; } = null!;

    public required string FromAddress { get; set; } = null!;

    public required string ToAddress { get; set; } = null!;

    public required decimal Amount { get; set; }

    public string? CallData { get; set; }
}
