using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class TokenPrice : EntityBase<int>
{
    public decimal PriceInUsd { get; set; }

    public string ExternalId { get; set; } = null!;

    public DateTimeOffset LastUpdated { get; set; }
}
