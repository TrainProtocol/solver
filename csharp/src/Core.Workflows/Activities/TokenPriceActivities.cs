using Temporalio.Activities;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.Abstractions.Repositories;

namespace Train.Solver.Core.Workflows.Activities;

public class TokenPriceActivities(
    INetworkRepository networkRepository,
    ITokenPriceService tokenPriceService)
{
    [Activity]
    public async Task<Dictionary<string, decimal>> GetTokensPricesAsync()
    {
        return await tokenPriceService.GetPricesAsync();
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
