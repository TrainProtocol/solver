using Train.Solver.Core.Abstractions.Entities.Base;

namespace Train.Solver.Core.Abstractions.Entities;

public enum AccountType
{
    LP,
    Charging,
}

public class ManagedAccount : EntityBase<int>
{
    public string Address { get; set; } = null!;

    public AccountType Type { get; set; }

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
}
