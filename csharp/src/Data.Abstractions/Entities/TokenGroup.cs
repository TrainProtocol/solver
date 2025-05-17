using System;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class TokenGroup : EntityBase<int>
{
    public string Asset { get; set; } = null!;
}