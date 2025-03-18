using Microsoft.EntityFrameworkCore;
using Serilog;
using Temporalio.Activities;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services;
using Train.Solver.Data;

namespace Train.Solver.WorkflowRunner.Activities;

public class TokenPriceActivities(
    SolverDbContext dbContext,
    TokenMarketPriceService tokenMarketPriceProvider)
{
    [Activity]
    public async Task<Dictionary<string, TokenMarketPriceResponse>> GetTokensMarketPricesAsync()
    {
        var tokenPrices = await dbContext.TokenPrices.ToListAsync();
        var currencySymbolsRaw = string.Join(",", tokenPrices.Where(x => x.ApiSymbol != null).Select(x => x.ApiSymbol));

        var tokenPricesResult = await tokenMarketPriceProvider.GetCoingeckoPricesAsync(currencySymbolsRaw);

        if (tokenPricesResult.IsFailed)
        {
            throw new Exception(tokenPricesResult.Errors.First().Message);
        }

        return tokenPricesResult.Value;
    }

    [Activity]
    public async Task UpdateTokenPricesAsync(Dictionary<string, TokenMarketPriceResponse> tokenMarketPrices)
    {
        var apiSymbols = tokenMarketPrices.Keys.ToList();

        var updateableTokenPrices = await dbContext.TokenPrices
            .Where(x => x.ApiSymbol != null && apiSymbols.Contains(x.ApiSymbol))
            .ToListAsync();

        foreach (var tokenPrice in updateableTokenPrices)
        {
            tokenPrice.PriceInUsd = tokenMarketPrices[tokenPrice.ApiSymbol!].Usd;
            tokenPrice.LastUpdated = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    [Activity]
    public async Task CheckStaledTokensAsync()
    {
        var tokenPrices = await dbContext.TokenPrices
            .Where(x => x.LastUpdated <= DateTimeOffset.UtcNow.AddMinutes(10))
            .ToListAsync();

        foreach (var tokenPrice in tokenPrices)
        {
            Log.Information($"Tokens Price for {tokenPrice.ApiSymbol} is stale. Last updated {tokenPrice.LastUpdated}. {{alert}}", AlertChannel.AtomicSecondary);
        }
    }
}
