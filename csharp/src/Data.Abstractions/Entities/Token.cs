﻿using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class Token : EntityBase<int>
{
    public string Asset { get; set; } = null!;

    public string? TokenContract { get; set; }

    public bool IsNative { get; set; }

    public int Precision { get; set; }

    public int Decimals { get; set; }

    public int NetworkId { get; set; }

    public string Logo { get; set; } = null!;

    public virtual Network Network { get; set; } = null!;

    public int TokenPriceId { get; set; }

    public virtual TokenPrice TokenPrice { get; set; } = null!;
}
