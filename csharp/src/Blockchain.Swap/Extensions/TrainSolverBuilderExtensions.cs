using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Swap.Activities;
using Train.Solver.Blockchain.Swap.Workflows;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Blockchain.Swap.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoreWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(Constants.CoreTaskQueue)
            .AddTransientActivities<RouteActivities>()
            .AddTransientActivities<SwapActivities>()
            .AddTransientActivities<TokenPriceActivities>()
            .AddTransientActivities<WorkflowActivities>()
            .AddTransientActivities<NetworkActivities>()
            .AddWorkflow<SwapWorkflow>()
            .AddWorkflow<TokenPriceUpdaterWorkflow>()
            .AddWorkflow<RouteStatusUpdaterWorkflow>()
            .AddWorkflow<EventListenerUpdaterWorkflow>();

        return builder;
    }
}