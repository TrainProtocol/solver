using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Common.Worklows;
using Train.Solver.Blockchain.Swap.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

[Workflow]
public class EventListenerUpdaterWorkflow : IScheduledWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var activeNetworks = await ExecuteActivityAsync(
            (RouteActivities x) => x.GetActiveSolverRouteSourceNetworksAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        var activeEventListenerWorkflowIds = await ExecuteActivityAsync(
            (WorkflowActivities x) => x.GetRunningWorkflowIdsAsync(nameof(EventListenerWorkflow)),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        foreach (var network in activeNetworks)
        {
            if (!activeEventListenerWorkflowIds.Any(x => x == TemporalHelper.BuildEventListenerId(network.Name)))
            {
                await ExecuteActivityAsync(
                    (WorkflowActivities x) => x.RunEventListeningWorkflowAsync(
                        network.Name,
                        network.Type,
                        20,
                        TimeSpan.FromSeconds(5)),
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
                (WorkflowActivities x) => x.TerminateWorkflowAsync(eventListenerId),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}