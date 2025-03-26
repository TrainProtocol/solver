namespace Train.Solver.Logging.OpenTelemetry;

public class OpenTelemetryOptions
{
    public Uri OpenTelemetryUrl { get; set; } = null!;

    public string? SignozIngestionKey { get; set; }
}
