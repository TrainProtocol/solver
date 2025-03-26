using Temporalio.Extensions.Hosting;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Worklows;
using Train.Solver.WorkflowRunner.Solana.Activities;
using Train.Solver.WorkflowRunner.Solana.Workflows;

namespace Train.Solver.WorkflowRunner.Solana.Extensions;

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