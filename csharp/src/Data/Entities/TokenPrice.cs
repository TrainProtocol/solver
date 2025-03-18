using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public class TokenPrice : EntityBase<int>
{
    public decimal PriceInUsd { get; set; }

    public string? ApiSymbol { get; set; }

    public DateTimeOffset LastUpdated { get; set; }
}
