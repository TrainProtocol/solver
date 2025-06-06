﻿using Train.Solver.Blockchain.EVM.Extensions;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Data.Npgsql.Extensions;
using Train.Solver.Infrastructure.Logging.OpenTelemetry;
using Train.Solver.Infrastructure.Secret.HashicorpKeyVault;

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
            .WithNpgsqlRepositories()
            .WithHashicorpKeyVault()
            .WithEVMWorkflows();
    })
    .Build();

var scope = host.Services.CreateScope();

await host.RunAsync();
