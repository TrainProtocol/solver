using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Services;
using Train.Solver.Data.Entities;
using Train.Solver.Data.Extensions;

namespace Train.Solver.Core.DependencyInjection;

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
        configuration.GetSection("TrainSolver").Bind(options);

        configureOptions?.Invoke(options);

        services.Configure<TrainSolverOptions>(configuration.GetSection("TrainSolver")); // Bind from config

        if (configureOptions != null)
        {
            services.Configure(configureOptions); // Apply manual overrides
        }

        services.AddHttpClient();

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options.RedisConnectionString));
        services.AddTransient(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(options.RedisDatabaseIndex));
        services.AddSingleton<IDistributedLockFactory>(x => RedLockFactory.Create(new List<RedLockMultiplexer>
        {
            new(ConnectionMultiplexer.Connect(options.RedisConnectionString))
        }));

        services.AddData(options.DatabaseConnectionString);
        services.AddTemporalClient(options.TemporalServerHost, options.TemporalNamespace);
        services.AddBlockchainServices();

        services.AddTransient<RouteService>();
        services.AddTransient<TokenMarketPriceService>();

        return new TrainSolverBuilder(services, options);
    }    

    private static IServiceCollection AddBlockchainServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetAssembly(typeof(IBlockchainService));

        if (assembly is null)
            throw new Exception("Could not find the assembly containing the blockchain services");

        var blockchainTypes = assembly.GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && !t.IsInterface
                        && t.IsAssignableTo(typeof(IBlockchainService)))
            .ToList();

        foreach (var implementation in blockchainTypes)
        {
            var interfaceTypes = implementation.GetInterfaces()
                .Where(i => typeof(IBlockchainService).IsAssignableFrom(i))
                .DistinctBy(i => i.Name)
                .ToList();

            if (interfaceTypes.Any())
            {
                var networkGroupProperty =
                    implementation.GetProperty(nameof(NetworkGroup), BindingFlags.Static | BindingFlags.Public);
                if (networkGroupProperty == null)
                {
                    throw new Exception(
                        $"The type {implementation.Name} must have a public static 'NetworkGroup' property.");
                }

                var networkGroup = networkGroupProperty.GetValue(null);

                foreach (var interfaceType in interfaceTypes)
                {
                    services.AddKeyedTransient(interfaceType, networkGroup, implementation);
                }
            }
        }

        return services;
    }

    private static IServiceCollection AddTemporalClient(
        this IServiceCollection services,
        string serverHost, string @namespace)
    {
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

    private static TrainSolverBuilder WithDataDogLogging(
        this TrainSolverBuilder builder,
        IConfiguration configuration,
        LogEventLevel minimumLogLevel = LogEventLevel.Information)
    {
        var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"];
        var service = configuration["WEBSITE_SITE_NAME"] ?? configuration["APP_NAME"];
        var loggingMinLevel = configuration["LOGGING_MIN_LEVEL"];

        var serilogMinLogLevel = minimumLogLevel;

        if (loggingMinLevel is not null
            && int.TryParse(loggingMinLevel, out var logginvMinLevelValue))
        {
            serilogMinLogLevel = (LogEventLevel)logginvMinLevelValue;
        }

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(serilogMinLogLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.DatadogLogs(
                configuration["DataDog:ApiKey"],
                configuration: new(),
                service: service,
                tags: new[] { $"env:{env}" });

        Log.Logger = loggerConfig.CreateLogger();

        return builder;
    }
}