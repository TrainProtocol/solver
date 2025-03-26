using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Logging.OpenTelemetry;

public class OpenTelemetryOptions
{
    public Uri OpenTelemetryExplorerUrl { get; set; } = null!;
}
