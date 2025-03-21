using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Extensions.Hosting;
using Temporalio.Workflows;
using Train.Solver.Core.Data;
using Train.Solver.Core.Services;
using Train.Solver.Core.Workflows;

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
        configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        configureOptions?.Invoke(options);

        services.Configure<TrainSolverOptions>(configuration.GetSection(TrainSolverOptions.SectionName));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddHttpClient();

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options.RedisConnectionString));
        services.AddTransient(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(options.RedisDatabaseIndex));
        services.AddSingleton<IDistributedLockFactory>(x => RedLockFactory.Create(new List<RedLockMultiplexer>
        {
            new(ConnectionMultiplexer.Connect(options.RedisConnectionString))
        }));

        services.AddDbContext(options.DatabaseConnectionString);

        if (options.MigrateDatabase)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SolverDbContext>();
                dbContext.Database.Migrate();
            }
        }

        services.AddTemporalWorkerClient(options.TemporalServerHost, options.TemporalNamespace);

        services.AddTransient<RouteService>();



        return new TrainSolverBuilder(services, configuration, options);
    }

    public static TrainSolverBuilder WithTemporalWorkflows(
       this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services.AddHostedTemporalWorker(Constants.CSharpTaskQueue);
        var workflowsAssemblyTypes = typeof(SwapWorkflow).Assembly.GetTypes();

        var activities = workflowsAssemblyTypes
            .Where(x => x.GetMethods().Any(y => y.GetCustomAttributes(typeof(ActivityAttribute), inherit: false).Any()))
                .ToList();

        activities.ForEach(x => temporalBuilder.AddTransientActivities(x));

        var workflows = workflowsAssemblyTypes
            .Where(x => x.GetCustomAttributes(typeof(WorkflowAttribute), inherit: false).Any())
                .ToList();

        workflows.ForEach(x => temporalBuilder.AddWorkflow(x));

        return builder;
    }

    private static IServiceCollection AddTemporalWorkerClient(
        this IServiceCollection services,
        string serverHost, string @namespace)
    {
        services
        .AddTemporalClient(serverHost, @namespace);

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

    private static IServiceCollection AddDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SolverDbContext>(
            options => options.UseNpgsql(connectionString));

        return services;
    }
}