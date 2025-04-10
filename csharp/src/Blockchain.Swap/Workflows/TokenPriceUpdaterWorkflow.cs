using Temporalio.Workflows;
using Train.Solver.Blockchain.Abstractions.Workflows;
using Train.Solver.Blockchain.Common;
using Train.Solver.Blockchain.Common.Helpers;
using Train.Solver.Blockchain.Swap.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Blockchain.Swap.Workflows;

[Workflow]
public class TokenPriceUpdaterWorkflow : IScheduledWorkflow
{
    public static ActivityOptions GetMarketPriceActivityOptions = new()
    {
        ScheduleToCloseTimeout = TimeSpan.FromDays(2),
        RetryPolicy = new()
        {
            InitialInterval = TimeSpan.FromSeconds(10),
            MaximumAttempts = 5,
            BackoffCoefficient = 1,
        }
    };
    [WorkflowRun]
    public async Task RunAsync()
    {
        try
        {
            var tokenMarketPrices = await ExecuteActivityAsync(
                (TokenPriceActivities x) => x.GetTokensPricesAsync(),
                GetMarketPriceActivityOptions);

            await ExecuteActivityAsync(
                (TokenPriceActivities x) => x.UpdateTokenPricesAsync(tokenMarketPrices),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
        catch (Exception ex)
        {
        }

        await ExecuteActivityAsync(
            (TokenPriceActivities x) => x.CheckStaledTokensAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
    }
}
