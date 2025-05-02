using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Common.Worklows;
using Train.Solver.Blockchain.Solana.Activities;
using Train.Solver.Blockchain.Solana.Workflows;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Blockchain.Solana.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithSolanaWorkflows(
        this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.Solana))
            .AddWorkflow<SolanaTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddTransientActivities<SolanaBlockchainActivities>()
            .AddTransientActivities<UtilityActivities>();

        return builder;
    }
}
