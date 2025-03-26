﻿using Serilog;
using Serilog.Extensions.Logging;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Repositories.Npgsql.Extensions;
using Train.Solver.Secret.AzureKeyVault;
using Train.Solver.TokenPrice.Coingecko;
using Train.Solver.Workflows.Extensions;

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
            .WithCoreWorkflows()
            .WithAzureKeyVault()
            .WithCoingeckoPrices();

        services.AddLogging(loggingBuilder => loggingBuilder
            .ClearProviders()
            .AddSerilog(dispose: true)
            .AddFilter<SerilogLoggerProvider>("Microsoft", LogLevel.None)
            .AddFilter<SerilogLoggerProvider>("System", LogLevel.None)
            .AddFilter<SerilogLoggerProvider>("Azure", LogLevel.None));
    })
    .Build();

await host.RunAsync();
