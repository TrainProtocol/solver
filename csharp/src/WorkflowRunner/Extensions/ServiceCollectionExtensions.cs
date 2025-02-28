using Temporalio.Activities;
using Temporalio.Extensions.Hosting;
using Temporalio.Workflows;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.WorkflowRunner.Workflows;

namespace Train.Solver.WorkflowRunner.Extensions;

public static class ServiceCollectionExtensions
{
    public static TrainSolverBuilder AddTemporalWorkflows(
       this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services.AddHostedTemporalWorker(Constants.CSharpTaskQueue);
        var workflowsAssemblyTypes = typeof(SwapWorkflow).Assembly.GetTypes();

        // add all activities and workflows
        workflowsAssemblyTypes
            .Where(x => x.GetMethods().Any(y => y.GetCustomAttributes(typeof(ActivityAttribute), inherit: false).Any()))
                .ToList()
                .ForEach(x => temporalBuilder.AddTransientActivities(x));

        workflowsAssemblyTypes
            .Where(x => x.GetCustomAttributes(typeof(WorkflowAttribute), inherit: false).Any())
                .ToList()
                .ForEach(x => temporalBuilder.AddWorkflow(x));

        return builder;
    }
}
