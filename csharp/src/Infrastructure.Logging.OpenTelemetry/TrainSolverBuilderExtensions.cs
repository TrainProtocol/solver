using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection.PortableExecutable;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Infrastructure.Logging.OpenTelemetry;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithOpenTelemetryLogging(
        this TrainSolverBuilder builder, string serviceName)
    {
        return builder.WithOpenTelemetryLogging(null, serviceName);
    }

    public static TrainSolverBuilder WithOpenTelemetryLogging(
        this TrainSolverBuilder builder, Action<OpenTelemetryOptions>? configureOptions, string serviceName)
    {
        var options = new OpenTelemetryOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if (options.OpenTelemetryUrl == null)
        {
            throw new InvalidOperationException("OpenTelemetryUrl is required");
        }

        configureOptions?.Invoke(options);

        // Configure OTLP options class which is shared by logging, metrics, and tracing
        builder.Services.Configure<OtlpExporterOptions>(ot =>
        {
            ot.Endpoint = options.OpenTelemetryUrl;

            if (!string.IsNullOrEmpty(options.SignozIngestionKey))
            {
                ot.Headers = $"{options.SignozIngestionHeaderKey}={options.SignozIngestionKey}";
            }
        });

        builder.Services.AddLogging(builder =>
        {
            builder
                .ClearProviders()
                .AddFilter("Microsoft", LogLevel.None)
                .AddFilter("System", LogLevel.None)
                .AddOpenTelemetry(logging =>
                {
                    logging.AddOtlpExporter();
                });
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(
                resource => resource.AddTelemetrySdk()
                   .AddEnvironmentVariableDetector()
                   .AddContainerDetector()
                   .AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(oi => {
                    oi.RecordException = true;
                })
                .AddOtlpExporter());

        return builder;
    }
}
