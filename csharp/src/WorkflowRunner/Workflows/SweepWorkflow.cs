using Temporalio.Workflows;
using Train.Solver.WorkflowRunner.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

[Workflow]
public class SweepWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var nonRefundedSwapIds = await ExecuteActivityAsync(
            (SwapActivities x) => x.GetNonRefundedSwapIdsAsync(),
            Constants.DefaultActivityOptions);

        foreach (var nonRefundedId in nonRefundedSwapIds)
        {
            await ExecuteActivityAsync(
                (BlockchainActivities x) => x.RefundAsync(nonRefundedId),
                Constants.DefaultActivityOptions);
        }
    }
}