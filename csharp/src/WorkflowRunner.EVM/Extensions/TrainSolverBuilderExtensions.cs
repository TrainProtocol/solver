using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchains.EVM.Activities;
using Train.Solver.Blockchains.EVM.Workflows;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;

namespace Train.Solver.Blockchains.EVM.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithEVMWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.EVM))
            .AddWorkflow<EVMTransactionProcessor>()
            .AddTransientActivities<EVMBlockchainActivities>();

        return builder;
    }
}