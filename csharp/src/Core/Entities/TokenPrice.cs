using Train.Solver.Core.Entities.Base;

namespace Train.Solver.Core.Entities;

public class TokenPrice : EntityBase<int>
{
    public decimal PriceInUsd { get; set; }

    public string? ApiSymbol { get; set; }

    public DateTimeOffset LastUpdated { get; set; }
}
