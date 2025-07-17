using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class RateProvider : EntityBase
{
    public string Name { get; set; } = null!;
}
