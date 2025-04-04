using System.Text.Json;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.EVM.Extensions;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Infrastructure.Secret.AzureKeyVault;
using Train.Solver.Data.Npgsql.Extensions;
using Train.Solver.Blockchain.EVM.Activities;

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
            .WithEVMWorkflows();
    })
    .Build();

var scope = host.Services.CreateScope();

await host.RunAsync();
