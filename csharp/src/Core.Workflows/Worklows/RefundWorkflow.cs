using Temporalio.Workflows;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows.Worklows;

[Workflow]
public class RefundWorkflow
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