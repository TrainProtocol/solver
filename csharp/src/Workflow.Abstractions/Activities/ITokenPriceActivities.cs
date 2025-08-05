
using Temporalio.Activities;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface ITokenPriceActivities
{
    [Activity]
    Task CheckStaledTokensAsync();

    [Activity]
    Task<Dictionary<string, decimal>> GetTokensPricesAsync();

    [Activity]
    Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices);
}