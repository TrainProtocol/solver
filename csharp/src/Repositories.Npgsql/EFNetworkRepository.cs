using Microsoft.EntityFrameworkCore;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Repositories;

namespace Train.Solver.Repositories.Npgsql;

public class EFNetworkRepository(SolverDbContext dbContext) : INetworkRepository
{
    public async Task<Token?> GetTokenAsync(string networkName, string asset)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .FirstOrDefaultAsync(x => x.Asset == asset && x.Network.Name == networkName);
    }

    public async Task<Token?> GetTokenByContractAsync(string networkName, string contractAddress)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .FirstOrDefaultAsync(x => x.TokenContract == contractAddress && x.Network.Name == networkName);
    }

    public async Task<Token?> GetNativeTokenAsync(string networkName)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .FirstOrDefaultAsync(x => x.Network.Name == networkName && x.IsNative);
    }

    public async Task<Dictionary<string, Token>> GetTokensAsync(string networkName, string[] assets)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .Where(x => x.Network.Name == networkName && assets.Contains(x.Asset))
            .ToDictionaryAsync(x => x.Asset);
    }

    public async Task<Network?> GetAsync(string networkName)
    {
        return await dbContext.Networks
            .Include(x => x.Contracts)
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.ManagedAccounts)
            .Include(x => x.Nodes)
            .FirstOrDefaultAsync(x => x.Name == networkName);
    }

    public async Task<List<Network>> GetAllAsync()
    {
        return await dbContext.Networks
            .Include(x => x.Contracts)
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.ManagedAccounts)
            .Include(x => x.Nodes)
            .ToListAsync();
    }

    public async Task<Dictionary<string, Token>> GetNativeTokensAsync(string[] networkNames)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .Where(x => x.IsNative && networkNames.Contains(x.Network.Name))
            .ToDictionaryAsync(x => x.Network.Name);
    }

    public async Task<Dictionary<string, string>> GetSolverAccountsAsync(string[] networkNames)
    {
        return await dbContext.ManagedAccounts
            .Include(x => x.Network)
            .Where(x => networkNames.Contains(x.Network.Name) && x.Type == AccountType.LP)
            .ToDictionaryAsync(x => x.Network.Name, y => y.Address);
    }

    public async Task<List<Token>> GetTokensAsync()
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .ToListAsync();
    }

    public async Task<Dictionary<string, Token>> GetTokensBySymbolsAsync(string[] assets)
    {
        return await dbContext.Tokens
            .Include(x => x.Network)
            .Include(x => x.TokenPrice)
            .Where(x => assets.Contains(x.Asset))
            .ToDictionaryAsync(x => x.Asset);
    }

    public Task<Dictionary<string, Token>> GetTokensByExternalIdsAsync(string[] assets)
    {
        return dbContext.Tokens
            .Include(x => x.TokenPrice)
            .Where(x => assets.Contains(x.TokenPrice.ExternalId))
            .ToDictionaryAsync(x => x.TokenPrice.ExternalId);
    }

    public async Task UpdateTokenPricesAsync(Dictionary<string, decimal> prices)
    {
        var assets = prices.Keys.ToArray();
        var tokensByAsset = await GetTokensBySymbolsAsync(assets);

        foreach (var asset in prices.Keys)
        {
            if (tokensByAsset.TryGetValue(asset, out var token))
            {
                token.TokenPrice.PriceInUsd = prices[asset];
            }
        }

        await dbContext.SaveChangesAsync();
        return;
    }
}