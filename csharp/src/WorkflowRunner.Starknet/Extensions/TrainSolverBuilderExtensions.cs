using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchains.Starknet.Activities;
using Train.Solver.Blockchains.Starknet.Workflows;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;

namespace Train.Solver.Blockchains.Starknet.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithStarknetWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.Starknet))
            .AddWorkflow<StarknetTransactionProcessor>()
            .AddTransientActivities<StarknetBlockchainActivities>();

        return builder;
    }
}