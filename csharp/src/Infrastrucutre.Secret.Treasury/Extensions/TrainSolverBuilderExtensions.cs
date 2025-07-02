
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastrucutre.Secret.Treasury;
using Train.Solver.Infrastrucutre.Secret.Treasury.Client;
using Train.Solver.Infrastrucutre.Secret.Treasury.Options;

namespace Train.Solver.Infrastrucutre.Secret.Treasury.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithTreasury(
    this TrainSolverBuilder builder)
    {
        return builder.WithTreasury(null);
    }

    public static TrainSolverBuilder WithTreasury(
        this TrainSolverBuilder builder,
        Action<TreasuryOptions>? configureOptions)
    {
        var options = new TreasuryOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if (options.TreasuryUrl == null)
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
                c.BaseAddress = options.TreasuryUrl;
                c.Timeout = options.TreasuryTimeout;
            });

        builder.Services.AddTransient<IPrivateKeyProvider, TreasuryPrivateKeyProvider>();

        return builder;
    }
}
