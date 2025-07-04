﻿using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class Node : EntityBase<int>
{
    public string Url { get; set; } = null!;

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
}
