using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class ServiceFee : EntityBase
{
    public string? SourceNetwork { get; set; }

    public string? DestinationNetwork { get; set; }

    public string? SourceAsset { get; set; }

    public string? DestinationAsset { get; set; }

    public decimal FeeInUsd { get; set; }

    public decimal FeePercentage { get; set; } 
}
