using Temporalio.Extensions.Hosting;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Worklows;
using Train.Solver.WorkflowRunner.EVM.Activities;
using Train.Solver.WorkflowRunner.EVM.Workflows;

namespace Train.Solver.WorkflowRunner.EVM.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithEVMWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.EVM))
            .AddWorkflow<EVMTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddTransientActivities<EVMBlockchainActivities>()
            .AddTransientActivities<UtilityActivities>();

        return builder;
    }
}