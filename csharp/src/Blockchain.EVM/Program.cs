using Train.Solver.Blockchain.EVM.Extensions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Logging.OpenTelemetry;
using Train.Solver.Infrastructure.Treasury.Client.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
        builder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables())
    .ConfigureServices((hostContext, services) =>
    {
        services
            .AddTrainSolver(hostContext.Configuration)
            .WithOpenTelemetryLogging("EVM Runner")
            .WithTreasury()
            .WithEVMWorkflows();
    })
    .Build();

var scope = host.Services.CreateScope();

await host.RunAsync();
