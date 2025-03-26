﻿using Temporalio.Extensions.Hosting;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Workflows;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Worklows;

namespace Train.Solver.Workflows.Extensions;

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
            .AddWorkflow<SwapWorkflow>()
            .AddWorkflow<RouteStatusUpdaterWorkflow>()
            .AddWorkflow<EventListenerUpdaterWorkflow>();

        return builder;
    }
}