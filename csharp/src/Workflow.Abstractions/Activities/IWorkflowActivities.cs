using Temporalio.Activities;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface IWorkflowActivities
{
    [Activity]
    Task<List<string>> GetRunningWorkflowIdsAsync(string workflowType);

    [Activity]
    Task RunEventListeningWorkflowAsync(string networkName, uint blockBatchSize, int waitInterval);

    [Activity]
    Task StartRefundWorkflowAsync(string swapId);

    [Activity]
    Task<string> StartSwapWorkflowAsync(HTLCCommitEventMessage signal);

    [Activity]
    Task TerminateWorkflowAsync(string workflowId);
}
