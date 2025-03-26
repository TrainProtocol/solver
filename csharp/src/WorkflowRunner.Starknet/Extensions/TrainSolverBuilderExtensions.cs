using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchains.Starknet.Activities;
using Train.Solver.Blockchains.Starknet.Workflows;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Workflows.Worklows;

namespace Train.Solver.Blockchains.Starknet.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithStarknetWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.Starknet))
            .AddWorkflow<StarknetTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddTransientActivities<StarknetBlockchainActivities>();

        return builder;
    }
}