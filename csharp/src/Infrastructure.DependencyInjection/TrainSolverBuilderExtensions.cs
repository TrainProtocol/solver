using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Exceptions;
using Train.Solver.Common.Serialization;

namespace Train.Solver.Infrastructure.DependencyInjection;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder AddTrainSolver(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddTrainSolver(configuration, null);
    }

    public static TrainSolverBuilder AddTrainSolver(this IServiceCollection services,
        IConfiguration configuration,
        Action<TrainSolverOptions>? configureOptions)
    {
        var options = new TrainSolverOptions();
        configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        configureOptions?.Invoke(options);

        services.Configure<TrainSolverOptions>(configuration.GetSection(TrainSolverOptions.SectionName));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddTemporalWorkerClient(options.TemporalServerHost, options.TemporalNamespace);

        return new TrainSolverBuilder(services, configuration, options);
    }

    private static IServiceCollection AddTemporalWorkerClient(
        this IServiceCollection services,
        string serverHost, 
        string @namespace)
    {
        var dataConverter = new DataConverter(new DefaultPayloadConverter(new JsonSerializerOptions
        {
            Converters = { new BigIntegerConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        }), new DefaultFailureConverter());

        services.AddTemporalClient(otps =>
        {
            otps.TargetHost = serverHost;
            otps.Namespace = @namespace;
            otps.DataConverter = dataConverter;
        });

        var temporalClient = services.BuildServiceProvider().GetRequiredService<ITemporalClient>();

        var namespacesResponse = temporalClient.Connection.WorkflowService
            .ListNamespacesAsync(new())
            .GetAwaiter()
            .GetResult();

        if (!namespacesResponse.Namespaces.Any(x => x.NamespaceInfo.Name == @namespace))
        {
            try
            {
                temporalClient.Connection.WorkflowService.RegisterNamespaceAsync(new()
                {
                    Namespace = @namespace,
                    WorkflowExecutionRetentionPeriod =
                            Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.FromDays(7)),
                })
                .GetAwaiter()
                .GetResult();
            }
            catch (RpcException e) when (e.Code == RpcException.StatusCode.AlreadyExists)
            {
                //Namespace already exists
            }
        }

        return services;
    }
}
