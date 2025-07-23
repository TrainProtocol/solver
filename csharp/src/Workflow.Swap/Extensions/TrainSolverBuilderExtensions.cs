using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Swap.Activities;
using Train.Solver.Workflow.Swap.Workflows;

namespace Train.Solver.Workflow.Swap.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoreWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(Constants.CoreTaskQueue)
            .AddTransientActivities<UtilityActivities>()
            .AddTransientActivities<RouteActivities>()
            .AddTransientActivities<SwapActivities>()
            .AddTransientActivities<TokenPriceActivities>()
            .AddTransientActivities<WorkflowActivities>()
            .AddTransientActivities<NetworkActivities>()
            .AddWorkflow<SwapWorkflow>()
            .AddWorkflow<TransactionBuilderWorkflow>()
            .AddWorkflow<TokenPriceUpdaterWorkflow>()
            .AddWorkflow<RouteStatusUpdaterWorkflow>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddWorkflow<EventListenerUpdaterWorkflow>();

        return builder;
    }

    public static TrainSolverBuilder WithTemporalSchedules(
        this TrainSolverBuilder builder)
    {
        var client = builder.Services
            .BuildServiceProvider()
            .GetRequiredService<ITemporalClient>();

        client.RegisterTemporalSchedulesAsync()
            .GetAwaiter()
            .GetResult();

        return builder;
    }
}