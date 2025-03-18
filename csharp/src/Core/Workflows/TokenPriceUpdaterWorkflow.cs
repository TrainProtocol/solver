using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Helpers;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.Core.Workflows;

[Workflow]
public class TokenPriceUpdaterWorkflow
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
                TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "Failed to get currency market prices");
        }

        await ExecuteActivityAsync(
               (TokenPriceActivities x) => x.CheckStaledTokensAsync(),
               TemporalHelper.DefaultActivityOptions(Constants.CSharpTaskQueue));
    }
}
