using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Common.Worklows;
using Train.Solver.Blockchain.EVM.Activities;
using Train.Solver.Blockchain.EVM.Workflows;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Blockchain.EVM.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithEVMWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.EVM))
            .AddWorkflow<EVMTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddTransientActivities<WorkflowActivities>()
            .AddTransientActivities<EVMBlockchainActivities>()
            .AddTransientActivities<UtilityActivities>();

        return builder;
    }
}