
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Treasury.Client.Options;

namespace Train.Solver.Infrastructure.Treasury.Client.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithTreasuryClient(
        this TrainSolverBuilder builder,
        Action<TreasuryClientOptions>? configureOptions)
    {
        var options = new TreasuryClientOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if (options.TreasuryUri == null)
        {
            throw new InvalidOperationException("Treasury  URI is not set.");
        }

        configureOptions?.Invoke(options);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
            Buffered = true
        };

        builder.Services.AddRefitClient<ITreasuryClient>(refitSettings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = options.TreasuryUri;
                c.Timeout = options.TreasuryTimeout;
            });

        return builder;
    }
}
