using Microsoft.EntityFrameworkCore;
using Serilog;
using Temporalio.Activities;
using Train.Solver.Core.Data;
using Train.Solver.Core.Services.TokenPrice;

namespace Train.Solver.Core.Activities;

public class TokenPriceActivities(
    SolverDbContext dbContext,
    ITokenPriceService tokenPriceService)
{
    [Activity]
    public async Task<Dictionary<string, decimal>> GetTokensPricesAsync()
    {
        var symbols = await dbContext.TokenPrices.Select(x => x.ApiSymbol).ToArrayAsync();

        if(symbols.Length == 0)
        {
            return [];
        }

        var tokenPricesResult = await tokenPriceService.GetPricesAsync(symbols);

        return tokenPricesResult;
    }

    [Activity]
    public async Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices)
    {
        await tokenPriceService.UpdatePricesAsync(prices);
    }

    [Activity]
    public async Task CheckStaledTokensAsync()
    {
        var tokenPrices = await dbContext.TokenPrices
            .Where(x => x.LastUpdated <= DateTimeOffset.UtcNow.AddMinutes(10))
            .ToListAsync();

        foreach (var tokenPrice in tokenPrices)
        {
            Log.Information($"Tokens Price for {tokenPrice.ApiSymbol} is stale. Last updated {tokenPrice.LastUpdated}.");
        }
    }
}
