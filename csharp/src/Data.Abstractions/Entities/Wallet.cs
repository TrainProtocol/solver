﻿using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class Wallet : EntityBase<int>
{
    public string Name { get; set; }

    public string Address { get; set; } = null!;

    public NetworkType NetworkType { get; set; }

    public bool IsDefault { get; set; }
}
