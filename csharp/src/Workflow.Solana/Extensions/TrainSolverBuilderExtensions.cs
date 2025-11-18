using Temporalio.Extensions.Hosting;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Workflow.Solana.Activities;
using Train.Solver.Workflow.Solana.Workflows;

namespace Train.Solver.Workflow.Solana.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithSolanaWorkflows(
        this TrainSolverBuilder builder)
    {
        builder.Services
            .AddHostedTemporalWorker(builder.Options.NetworkType)
            .AddWorkflow<SolanaTransactionProcessor>()
            .AddTransientActivities<SolanaBlockchainActivities>();

        return builder;
    }
}
