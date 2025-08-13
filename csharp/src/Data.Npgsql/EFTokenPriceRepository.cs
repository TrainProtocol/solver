using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFTokenPriceRepository(SolverDbContext dbContext) : ITokenPriceRepository
{
    public async Task<TokenPrice?> CreateAsync(string symbol, string externalId)
    {
        var tokenPrice = new TokenPrice
        {
            Symbol = symbol,
            ExternalId = externalId,
        };

        dbContext.TokenPrices.Add(tokenPrice);
        await dbContext.SaveChangesAsync();

        return tokenPrice;
    }

    public async Task<List<TokenPrice>> GetAllAsync()
    {
        return await dbContext.TokenPrices.ToListAsync();
    }

    public async Task<TokenPrice?> GetAsync(string symbol)
    {
        return await dbContext.TokenPrices
            .FirstOrDefaultAsync(x => x.Symbol == symbol);
    }

    public async Task UpdateAsync(Dictionary<string, decimal> prices)
    {
        var externalIds = prices.Keys.ToArray();

        var tokenPrices = await dbContext.TokenPrices
            .Where(x => externalIds.Contains(x.ExternalId))
            .ToDictionaryAsync(x => x.ExternalId);

        foreach (var externalId in prices.Keys)
        {
            if (tokenPrices.TryGetValue(externalId, out var token))
            {
                token.PriceInUsd = prices[externalId];
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
