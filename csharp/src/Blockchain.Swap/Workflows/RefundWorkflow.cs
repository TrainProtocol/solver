using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Activities;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Swap.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

[Workflow]
public class RefundWorkflow : IScheduledWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var nonRefundedSwapIds = await ExecuteActivityAsync(
            (SwapActivities x) => x.GetNonRefundedSwapIdsAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));

        foreach (var nonRefundedId in nonRefundedSwapIds)
        {
            await ExecuteActivityAsync(
                (WorkflowActivities x) => x.StartRefundWorkflowAsync(nonRefundedId),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
    }
}