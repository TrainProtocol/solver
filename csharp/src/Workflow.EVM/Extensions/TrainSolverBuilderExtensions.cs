using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Extensions.Hosting;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Workflow.EVM.Workflows;
using Train.Solver.Workflow.EVM.Activities;
using Train.Solver.SmartNodeInvoker;
using Train.Solver.Workflow.EVM.Helpers;

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

        builder.Services.AddTransient<IFeeEstimatorFactory, FeeEstimatorFactory>();
        builder.Services.AddSmartNodeInvoker(builder.Options.RedisConnectionString, builder.Options.RedisDatabaseIndex);
        builder.Services.AddSingleton<IDistributedLockFactory>(x => RedLockFactory.Create(new List<RedLockMultiplexer>
        {
            new(ConnectionMultiplexer.Connect(builder.Options.RedisConnectionString))
        }));

        return builder;
    }
}