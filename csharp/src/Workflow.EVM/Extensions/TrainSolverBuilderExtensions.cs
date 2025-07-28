using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Extensions.Hosting;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Workflow.EVM.Workflows;
using Train.Solver.Workflow.EVM.Activities;

namespace Train.Solver.Workflow.EVM.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithEVMWorkflows(
        this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(builder.Options.NetworkType)
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