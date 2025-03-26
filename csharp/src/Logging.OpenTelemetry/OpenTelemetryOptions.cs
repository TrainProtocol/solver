﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Logging.OpenTelemetry;

public class OpenTelemetryOptions
{
    public Uri OpenTelemetryUrl { get; set; } = null!;
    public string? SignozIngestionKey { get; set; }
}
