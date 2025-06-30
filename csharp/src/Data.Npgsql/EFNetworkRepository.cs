using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFNetworkRepository(SolverDbContext dbContext) : INetworkRepository
{
    public async Task<Token?> GetTokenAsync(string networkName, string asset)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .FirstOrDefaultAsync(x => x.Asset == asset && x.Network.Name == networkName);
    }

    public async Task<Network?> GetAsync(string networkName)
    {
        return await dbContext.Networks
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.Nodes)
            .FirstOrDefaultAsync(x => x.Name == networkName);
    }

    public async Task<List<Network>> GetAllAsync()
    {
        return await dbContext.Networks
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.Nodes)
            .ToListAsync();
    }

    public async Task<string> GetSolverAccountAsync(string networkName)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name == networkName)
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new Exception($"Network '{networkName}' not found.");
        }

        var managedAccount = await dbContext.Wallets
            .Where(x => x.NetworkType == network.Type)
            .FirstOrDefaultAsync();

        if (managedAccount == null)
        {
            throw new Exception($"Solver account for network '{networkName}' not found.");
        }

        return managedAccount.Address;
    }

    public async Task<List<Token>> GetTokensAsync()
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .ToListAsync();
    }

    public async Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices)
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
        return;
    }
}