﻿namespace Train.Solver.Core.Models;

public class TokenModel
{
    public string NetworkName { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public int Id { get; set; }

    public bool IsNative { get; set; }

    public int Precision { get; set; }

    public decimal UsdPrice { get; set; }
}
