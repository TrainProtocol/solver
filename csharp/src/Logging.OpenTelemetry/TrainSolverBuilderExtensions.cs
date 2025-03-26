using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Train.Solver.Core.DependencyInjection;

namespace Train.Solver.Logging.OpenTelemetry;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithOpenTelemetryLogging(
    this TrainSolverBuilder builder)
    {
        return builder.WithOpenTelemetryLogging(null);
    }

    public static TrainSolverBuilder WithOpenTelemetryLogging(
        this TrainSolverBuilder builder, Action<OpenTelemetryOptions>? configureOptions)
    {
        var options = new OpenTelemetryOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if (options.OpenTelemetryExplorerUrl == null)
        {
            throw new InvalidOperationException("OpenTelemetryExplorerUrl is required");
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
                ot.SetResourceBuilder(ResourceBuilder.CreateDefault());
                ot.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = options.OpenTelemetryExplorerUrl;
                });
            });
        });
        return builder;
    }
}
