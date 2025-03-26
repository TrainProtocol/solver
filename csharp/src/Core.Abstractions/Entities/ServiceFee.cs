using Train.Solver.Core.Abstractions.Entities.Base;

namespace Train.Solver.Core.Abstractions.Entities;

public class ServiceFee : EntityBase<int>
{
    public string? SourceNetwork { get; set; }

    public string? DestinationNetwork { get; set; }

    public string? SourceAsset { get; set; }

    public string? DestinationAsset { get; set; }

    public decimal FeeInUsd { get; set; }

    public decimal FeePercentage { get; set; } 
}
