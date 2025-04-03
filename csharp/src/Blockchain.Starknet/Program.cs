using Train.Solver.Blockchain.Starknet.Extensions;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Infrastructure.Secret.AzureKeyVault;
using Train.Solver.Repositories.Npgsql.Extensions;

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
            .WithNpgsqlRepositories()
            .WithAzureKeyVault()
            .WithStarknetWorkflows();
    })
    .Build();

await host.RunAsync();
