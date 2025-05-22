using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public enum AccountType
{
    Primary,
    Secondary,
}

public class ManagedAccount : EntityBase<int>
{
    public string Address { get; set; } = null!;

    public AccountType Type { get; set; }

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
}
