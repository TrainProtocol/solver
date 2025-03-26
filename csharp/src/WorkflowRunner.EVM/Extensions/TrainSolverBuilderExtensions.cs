using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchains.EVM.Activities;
using Train.Solver.Blockchains.EVM.Workflows;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Worklows;

namespace Train.Solver.Blockchains.EVM.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithEVMWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.EVM))
            .AddWorkflow<EVMTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddWorkflow<WorkflowActivities>()
            .AddTransientActivities<EVMBlockchainActivities>();

        return builder;
    }
}