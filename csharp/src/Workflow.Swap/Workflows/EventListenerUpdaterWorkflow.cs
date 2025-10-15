using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Swap.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
[TemporalJobSchedule(Chron = "*/5 * * * *")]
public class EventListenerUpdaterWorkflow : IScheduledWorkflow
{
    private const int _waitIntervalInSeconds = 5;
    private const uint _blockBachSize = 5;

    [WorkflowRun]
    public async Task RunAsync()
    {
        var activeNetworks = await ExecuteActivityAsync(
            (IRouteActivities x) => x.GetActiveSolverRouteSourceNetworksAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        var activeEventListenerWorkflowIds = await ExecuteActivityAsync(
            (IWorkflowActivities x) => x.GetRunningWorkflowIdsAsync(nameof(EventListenerWorkflow)),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        foreach (var network in activeNetworks)
        {
            if (!activeEventListenerWorkflowIds.Any(x => x == TemporalHelper.BuildEventListenerId(network.Name)))
            {
                await ExecuteActivityAsync(
                    (WorkflowActivities x) => x.RunEventListeningWorkflowAsync(
                        network.Name,
                        _blockBachSize,
                        _waitIntervalInSeconds),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
            }
        }

        var mustBeStoppedEventListenersIds = activeEventListenerWorkflowIds
            .Where(workflowId =>
                !activeNetworks.Any(x => workflowId == TemporalHelper.BuildEventListenerId(x.Name)))
            .ToList();

        foreach (var eventListenerId in mustBeStoppedEventListenersIds)
        {
            await ExecuteActivityAsync(
                (IWorkflowActivities x) => x.TerminateWorkflowAsync(eventListenerId),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}
