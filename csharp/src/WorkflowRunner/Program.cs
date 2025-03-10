﻿using Serilog;
using Serilog.Extensions.Logging;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Secret;
using Train.Solver.WorkflowRunner.Extensions;

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
            .AddAzureKeyVaultStorage(hostContext.Configuration)
            .AddTemporalWorkflows();

        services.AddLogging(loggingBuilder => loggingBuilder
            .ClearProviders()
            .AddSerilog(dispose: true)
            .AddFilter<SerilogLoggerProvider>("Microsoft", LogLevel.None)
            .AddFilter<SerilogLoggerProvider>("System", LogLevel.None)
            .AddFilter<SerilogLoggerProvider>("Azure", LogLevel.None));
    })
    .Build();

await host.RunAsync();