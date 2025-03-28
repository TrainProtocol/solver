﻿using Train.Solver.Core.Extensions;
using Train.Solver.Repositories.Npgsql.Extensions;
using Train.Solver.Secret.AzureKeyVault;
using Train.Solver.WorkflowRunner.Solana.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
        builder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables())
    .ConfigureServices((hostContext, services) =>
    {
        services
            .AddTrainSolver(hostContext.Configuration)
            .WithNpgsqlRepositories()
            .WithAzureKeyVault()
            .WithSolanaWorkflows();
    })
    .Build();

await host.RunAsync();
