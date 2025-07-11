﻿using Microsoft.Extensions.Options;
using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchain.EVM.Activities;
using Train.Solver.Blockchain.EVM.Workflows;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.DependencyInjection;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Api.Cloud.Namespace.V1;
using Train.Solver.Infrastructure.Abstractions;

namespace Train.Solver.Blockchain.EVM.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithEVMWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.EVM))
            .AddWorkflow<EVMTransactionProcessor>()
            .AddTransientActivities<EVMBlockchainActivities>();

        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(builder.Options.RedisConnectionString));
        builder.Services.AddTransient(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(builder.Options.RedisDatabaseIndex));
        builder.Services.AddSingleton<IDistributedLockFactory>(x => RedLockFactory.Create(new List<RedLockMultiplexer>
        {
            new(ConnectionMultiplexer.Connect(builder.Options.RedisConnectionString))
        }));

        return builder;
    }
}