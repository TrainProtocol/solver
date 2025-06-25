using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class ManagedAccount : EntityBase<int>
{
    public string Address { get; set; } = null!;

    public NetworkType NetworkType { get; set; }
}
