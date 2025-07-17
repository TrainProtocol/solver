using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Workflows.Common;
using Train.Solver.Workflows.Common.Helpers;
using Train.Solver.Workflows.Swap.Activities;
using Train.Solver.Workflows.Swap.Workflows;

namespace Train.Solver.Workflows.Swap.Extensions;

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