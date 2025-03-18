using Temporalio.Workflows;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows;

[Workflow]
public class RefundWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var nonRefundedSwapIds = await ExecuteActivityAsync(
            (SwapActivities x) => x.GetNonRefundedSwapIdsAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));

        foreach (var nonRefundedId in nonRefundedSwapIds)
        {
            await ExecuteActivityAsync(
                (WorkflowActivities x) => x.StartRefundWorkflowAsync(nonRefundedId),
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        }
    }
}