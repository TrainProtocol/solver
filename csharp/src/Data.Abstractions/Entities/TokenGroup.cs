using System;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class TokenGroup : EntityBase
{
    public string Asset { get; set; } = null!;
}