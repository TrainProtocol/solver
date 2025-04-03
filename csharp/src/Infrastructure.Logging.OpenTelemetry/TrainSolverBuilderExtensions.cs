using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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


        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName: serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(ot =>
                    {
                        ot.Endpoint = options.OpenTelemetryUrl;
                        ot.Protocol = OtlpExportProtocol.Grpc;
                        
                        if (!string.IsNullOrEmpty(options.SignozIngestionKey))
                        {
                            string headerKey = "signoz-ingestion-key";
                            string headerValue = options.SignozIngestionKey;
                            string formattedHeader = $"{headerKey}={headerValue}";
                            ot.Headers = formattedHeader;
                        }
                    }));

        return builder;
    }
}
