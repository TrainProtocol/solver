using Temporalio.Workflows;
using Train.Solver.Workflows.Abstractions.Activities;
using Train.Solver.Workflows.Abstractions.Workflows;
using Train.Solver.Workflows.Common;
using Train.Solver.Workflows.Common.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Workflows.Swap.Workflows;

[Workflow]
[TemporalJobSchedule(Chron = "*/5 * * * *")]
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
            Logger.LogInformation("TokenPriceUpdaterWorkflow started");

            var tokenMarketPrices = await ExecuteActivityAsync(
                (ITokenPriceActivities x) => x.GetTokensPricesAsync(),
                GetMarketPriceActivityOptions);

            await ExecuteActivityAsync(
                (ITokenPriceActivities x) => x.UpdateTokenPricesAsync(tokenMarketPrices),
                TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
        }
        catch (Exception ex)
        {
        }

        await ExecuteActivityAsync(
            (ITokenPriceActivities x) => x.CheckStaledTokensAsync(),
            TemporalHelper.DefaultActivityOptions(Constants.CoreTaskQueue));
    }
}
