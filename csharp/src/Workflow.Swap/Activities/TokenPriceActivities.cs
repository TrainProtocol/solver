using Temporalio.Activities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Workflow.Abstractions.Activities;

namespace Train.Solver.Workflow.Swap.Activities;

public class TokenPriceActivities(
    ITokenPriceRepository tokenPriceRepository,
    ITokenPriceService tokenPriceService) : ITokenPriceActivities
{
    [Activity]
    public async Task<Dictionary<string, decimal>> GetTokensPricesAsync()
    {
        var tokenPrices = await tokenPriceRepository.GetAllAsync();
        var externalIds = tokenPrices.Select(x => x.ExternalId).ToArray();

        return await tokenPriceService.GetPricesAsync(externalIds);
    }

    [Activity]
    public async Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices)
    {
        await tokenPriceRepository.UpdateAsync(prices);
    }

    [Activity]
    public async Task CheckStaledTokensAsync()
    {
        var tokenPrices = await tokenPriceRepository.GetAllAsync();

        var tokensWithStaledPrices = tokenPrices.Where(x => x.LastUpdated <= DateTimeOffset.UtcNow.AddMinutes(10));

        foreach (var token in tokensWithStaledPrices)
        {
            //Log.Information($"Tokens Price for {token.TokenPrice.ExternalId} is stale. Last updated {token.TokenPrice.LastUpdated}.");
        }
    }
}
