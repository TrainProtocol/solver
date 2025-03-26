using Train.Solver.Core.Abstractions.Entities.Base;

namespace Train.Solver.Core.Abstractions.Entities;

public class TokenPrice : EntityBase<int>
{
    public decimal PriceInUsd { get; set; }

    public string ExternalId { get; set; } = null!;

    public DateTimeOffset LastUpdated { get; set; }
}
