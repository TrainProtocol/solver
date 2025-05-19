using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Common.Worklows;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

[Workflow]
public class EventListenerUpdaterWorkflow : IScheduledWorkflow
{
    private const int _waitIntervalInSeconds = 5;
    private const uint _blockBachSize = 20;

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
            var eventListnerId = TemporalHelper.BuildEventListenerId(network.Name);

            if (!activeEventListenerWorkflowIds.Any(x => x == eventListnerId))
            {
                await ExecuteActivityAsync(
                    (WorkflowActivities x) => x.RunEventListeningWorkflowAsync(
                        network.Name,
                        network.Type,
                        _blockBachSize,
                        _waitIntervalInSeconds),
                    TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

                Logger.EventListeningStarted(eventListnerId);
            }
        }

        var mustBeStoppedEventListenersIds = activeEventListenerWorkflowIds
            .Where(workflowId => !activeNetworks.Any(
                x => workflowId == TemporalHelper.BuildEventListenerId(x.Name)));

        foreach (var eventListenerId in mustBeStoppedEventListenersIds)
        {
            await ExecuteActivityAsync(
                (IWorkflowActivities x) => x.TerminateWorkflowAsync(eventListenerId),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

            Logger.EventListeningTerminated(eventListenerId);
        }
    }
}
