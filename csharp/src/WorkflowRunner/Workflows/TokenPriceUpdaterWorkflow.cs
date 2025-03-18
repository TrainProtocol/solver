using Temporalio.Workflows;
using Train.Solver.WorkflowRunner.Activities;
using static Temporalio.Workflows.Workflow;

namespace Train.Solver.WorkflowRunner.Workflows;

[Workflow]
public class TokenPriceUpdaterWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        try
        {
            var tokenMarketPrices = await ExecuteActivityAsync(
                (TokenPriceActivities x) => x.GetTokensMarketPricesAsync(),
                Constants.GetMarketPriceActivityOptions);

            await ExecuteActivityAsync(
                (TokenPriceActivities x) => x.UpdateTokenPricesAsync(tokenMarketPrices),
                Constants.DefaultActivityOptions);
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "Failed to get currency market prices");
        }

        await ExecuteActivityAsync(
               (TokenPriceActivities x) => x.CheckStaledTokensAsync(),
               Constants.DefaultActivityOptions);
    }
}
