using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Activities;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

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