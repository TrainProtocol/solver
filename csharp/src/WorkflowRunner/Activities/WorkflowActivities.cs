using Microsoft.Extensions.Options;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Train.Solver.WorkflowRunner.Workflows;

namespace Train.Solver.WorkflowRunner.Activities;

public class WorkflowActivities(ITemporalClient temporalClient)
{
    [Activity]
    public virtual async Task TerminateWorkflowAsync(string workflowId)
    {
        var handle = temporalClient.GetWorkflowHandle(workflowId);
        await handle.TerminateAsync();
    }

    [Activity]
    public async Task<List<string>> GetRunningWorkflowIdsAsync(string workflowType)
    {
        string query = $"WorkflowType='{workflowType}' AND ExecutionStatus='Running'";
        IAsyncEnumerable<WorkflowExecution> workflows = temporalClient.ListWorkflowsAsync(query);

        var runningWorkflowsIds = new List<string>();
        await foreach (var execution in workflows)
        {
            runningWorkflowsIds.Add(execution.Id);
        }

        return runningWorkflowsIds;
    }

    [Activity]
    public virtual async Task RunEventListeningWorkflowAsync(
        string networkName,
        uint blockBatchSize,
        TimeSpan waitInterval)
    {
        await temporalClient.StartWorkflowAsync(
            (EventListenerWorkflow x) =>
                x.RunAsync(
                    networkName,
                    blockBatchSize,
                    waitInterval,
                    null),
            new(id: EventListenerWorkflow.BuildWorkflowId(networkName),
            taskQueue: Constants.CSharpTaskQueue)
            {
                IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning
            });
    }
}
