using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Train.Solver.Core.DependencyInjection;
using OpenTelemetry.Trace;

namespace Train.Solver.Logging.OpenTelemetry;

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

        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();

            logging.AddOpenTelemetry(ot =>
            {
                ot.IncludeFormattedMessage = true;
                ot.IncludeScopes = true;
                ot.ParseStateValues = true;
                ot.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                ot.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = options.OpenTelemetryUrl;
                    otlp.Protocol = OtlpExportProtocol.Grpc;
                    
                    if(!string.IsNullOrEmpty(options.SignozIngestionKey))
                    {
                        string headerKey = "signoz-ingestion-key";
                        string headerValue = options.SignozIngestionKey;
                        string formattedHeader = $"{headerKey}={headerValue}";
                        otlp.Headers = formattedHeader;
                    }
                });
            });
        });
        return builder;
    }
}
