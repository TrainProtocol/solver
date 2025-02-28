using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

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
