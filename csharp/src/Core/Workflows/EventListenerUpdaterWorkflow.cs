using Serilog;
using Temporalio.Workflows;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows;

[Workflow]
public class EventListenerUpdaterWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        Log.Information($"{nameof(EventListenerUpdaterWorkflow)} is started");

        var activeNetworks = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetActiveSolverRouteSourceNetworksAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        var activeEventListenerWorkflowIds = await ExecuteActivityAsync(
            (WorkflowActivities x) => x.GetRunningWorkflowIdsAsync(nameof(EventListenerWorkflow)),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        foreach (var network in activeNetworks)
        {
            if (!activeEventListenerWorkflowIds.Any(x => x == EventListenerWorkflow.BuildWorkflowId(network.Name)))
            {
                await ExecuteActivityAsync(
                    (WorkflowActivities x) => x.RunEventListeningWorkflowAsync(
                        network.Name,
                        network.Group,
                        20,
                        TimeSpan.FromSeconds(5)),
                    TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
            }
        }

        var mustBeStoppedEventListenersIds = activeEventListenerWorkflowIds
            .Where(workflowId =>
                !activeNetworks.Any(x => workflowId == EventListenerWorkflow.BuildWorkflowId(x.Name)))
            .ToList();

        foreach (var eventListenerId in mustBeStoppedEventListenersIds)
        {
            await ExecuteActivityAsync(
                (WorkflowActivities x) => x.TerminateWorkflowAsync(eventListenerId),
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        }
    }
}