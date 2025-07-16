using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class ServiceFee : EntityBase
{
    public decimal FeeInUsd { get; set; }

    public decimal FeePercentage { get; set; } 
}
