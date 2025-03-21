using Microsoft.EntityFrameworkCore;
using Train.Solver.Core.Data;

namespace Train.Solver.Core.Services.TokenPrice;

public abstract class TokenPriceServiceBase(SolverDbContext dbContext) : ITokenPriceService
{
    public abstract Task<Dictionary<string, decimal>> GetPricesAsync(params string[] tokenSymbols);

    public async Task UpdatePricesAsync(Dictionary<string, decimal> prices)
    {
        var apiSymbols = prices.Keys.ToList();

        var updateableTokenPrices = await dbContext.TokenPrices
            .Where(x => x.ApiSymbol != null && apiSymbols.Contains(x.ApiSymbol))
            .ToListAsync();

        foreach (var tokenPrice in updateableTokenPrices)
        {
            tokenPrice.PriceInUsd = prices[tokenPrice.ApiSymbol!];
            tokenPrice.LastUpdated = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }
}
