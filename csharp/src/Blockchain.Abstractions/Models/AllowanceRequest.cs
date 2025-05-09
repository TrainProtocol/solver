﻿namespace Train.Solver.Blockchain.Abstractions.Models;

public class AllowanceRequest : BaseRequest
{
    public string OwnerAddress { get; set; } = null!;

    public string Asset { get; set; } = null!;
}
