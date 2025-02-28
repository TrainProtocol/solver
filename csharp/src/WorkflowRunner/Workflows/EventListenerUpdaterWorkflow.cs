using Serilog;
using Temporalio.Workflows;
using Train.Solver.WorkflowRunner.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

[Workflow]
public class EventListenerUpdaterWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        Log.Information($"{nameof(EventListenerUpdaterWorkflow)} is started");

        var activeNetworkNames = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetActiveSolverRouteSourceNetworkNamesAsync(),
            Constants.DefaultActivityOptions);

        var activeEventListenerWorkflowIds = await ExecuteActivityAsync(
            (WorkflowActivities x) => x.GetRunningWorkflowIdsAsync(nameof(EventListenerWorkflow)),
            Constants.DefaultActivityOptions);

        foreach (var networkName in activeNetworkNames)
        {
            if (!activeEventListenerWorkflowIds.Any(x => x == EventListenerWorkflow.BuildWorkflowId(networkName)))
            {
                await ExecuteActivityAsync(
                    (WorkflowActivities x) => x.RunEventListeningWorkflowAsync(
                        networkName,
                        20,
                        TimeSpan.FromSeconds(5)),
                    Constants.DefaultActivityOptions);
            }
        }

        var mustBeStoppedEventListenersIds = activeEventListenerWorkflowIds
            .Where(workflowId =>
                !activeNetworkNames.Any(networkName => workflowId == EventListenerWorkflow.BuildWorkflowId(networkName)))
            .ToList();

        foreach (var eventListenerId in mustBeStoppedEventListenersIds)
        {
            await ExecuteActivityAsync(
                (WorkflowActivities x) => x.TerminateWorkflowAsync(eventListenerId),
                Constants.DefaultActivityOptions);
        }
    }
}