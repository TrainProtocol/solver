using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Common.Worklows;
using Train.Solver.Blockchain.Starknet.Activities;
using Train.Solver.Blockchain.Starknet.Workflows;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Blockchain.Starknet.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithStarknetWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.Starknet))
            .AddWorkflow<StarknetTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddTransientActivities<StarknetBlockchainActivities>()
            .AddTransientActivities<UtilityActivities>();

        return builder;
    }
}