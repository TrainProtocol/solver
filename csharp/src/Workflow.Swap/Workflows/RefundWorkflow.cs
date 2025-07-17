using Temporalio.Workflows;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Workflows;
using Train.Solver.Workflow.Common;
using Train.Solver.Workflow.Common.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflow.Swap.Workflows;

[Workflow]
public class RefundWorkflow : IScheduledWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var nonRefundedSwapIds = await ExecuteActivityAsync(
            (ISwapActivities x) => x.GetNonRefundedSwapIdsAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        foreach (var nonRefundedId in nonRefundedSwapIds)
        {
            await ExecuteActivityAsync(
                (IWorkflowActivities x) => x.StartRefundWorkflowAsync(nonRefundedId),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}