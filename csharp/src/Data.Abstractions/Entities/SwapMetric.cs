using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public class SwapMetric : EntityBase
{
    public string SourceNetwork { get; set; } = null!;

    public string SourceToken { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationToken { get; set; } = null!;

    public decimal VolumeInUsd { get; set; }

    public decimal ProfitInUsd { get; set; }

    public int SwapId { get; set; }

    public Swap Swap { get; set; } = null!;
}
