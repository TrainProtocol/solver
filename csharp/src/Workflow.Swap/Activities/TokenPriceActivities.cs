using Temporalio.Activities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Workflow.Abstractions.Activities;

namespace Train.Solver.Workflow.Swap.Activities;

public class TokenPriceActivities(
    INetworkRepository networkRepository,
    ITokenPriceService tokenPriceService) : ITokenPriceActivities
{
    [Activity]
    public async Task<Dictionary<string, decimal>> GetTokensPricesAsync()
    {
        var tokens = await networkRepository.GetTokensAsync();

        var externalIds = tokens
            .Where(x => x.TokenPrice != null && !string.IsNullOrEmpty(x.TokenPrice.ExternalId))
            .Select(x => x.TokenPrice.ExternalId)
            .ToArray();

        return await tokenPriceService.GetPricesAsync(externalIds);
    }

    [Activity]
    public async Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices)
    {
        await networkRepository.UpdateTokenPricesAsync(prices);
    }

    [Activity]
    public async Task CheckStaledTokensAsync()
    {
        var tokens = await networkRepository.GetTokensAsync();

        var tokensWithStaledPrices = tokens.Where(x => x.TokenPrice.LastUpdated <= DateTimeOffset.UtcNow.AddMinutes(10));

        foreach (var token in tokensWithStaledPrices)
        {
            //Log.Information($"Tokens Price for {token.TokenPrice.ExternalId} is stale. Last updated {token.TokenPrice.LastUpdated}.");
        }
    }
}
