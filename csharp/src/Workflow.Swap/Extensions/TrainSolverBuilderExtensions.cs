using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Swap.Activities;
using Train.Solver.Workflow.Swap.Workflows;
using Train.Solver.Workflow.Swap.Workflows.RedeemWorkflows;

namespace Train.Solver.Workflow.Swap.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoreWorkflows(
        this TrainSolverBuilder builder)
    {
        builder.Services
            .AddHostedTemporalWorker(Constants.CoreTaskQueue)
            .AddTransientActivities<UtilityActivities>()
            .AddTransientActivities<CacheActivities>()
            .AddTransientActivities<RouteActivities>()
            .AddTransientActivities<WalletActivities>()
            .AddTransientActivities<SwapActivities>()
            .AddTransientActivities<TokenPriceActivities>()
            .AddTransientActivities<WorkflowActivities>()
            .AddTransientActivities<NetworkActivities>()
            .AddWorkflow<SwapWorkflow>()
            .AddWorkflow<TransactionBuilderWorkflow>()
            .AddWorkflow<TokenPriceUpdaterWorkflow>()
            .AddWorkflow<RouteStatusUpdaterWorkflow>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddWorkflow<RefundWorkflow>()
            .AddWorkflow<BalanceUpdaterWorkflow>()
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