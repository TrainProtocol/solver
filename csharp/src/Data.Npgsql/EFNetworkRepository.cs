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

    public async Task<IEnumerable<Network>> GetAllAsync()
    {
        return await dbContext.Networks
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.Nodes)
            .ToListAsync();
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
    }

    public async Task<Network?> CreateAsync(string networkName, string displayName, NetworkType type, TransactionFeeType feeType, string chainId, int feePercentageIncrease, string htlcNativeContractAddress, string htlcTokenContractAddress)
    {
        var networkExists = await dbContext.Networks.AnyAsync(x => x.Name == networkName);

        if (networkExists)
        {
            return null;
        }

        var network = new Network
        {
            Name = networkName,
            ChainId = chainId,
            DisplayName = displayName,
            FeePercentageIncrease = feePercentageIncrease,
            FeeType = feeType,
            HTLCNativeContractAddress = htlcNativeContractAddress,
            HTLCTokenContractAddress = htlcTokenContractAddress,
            Type = type,
        };

        dbContext.Networks.Add(network);
        await dbContext.SaveChangesAsync();

        return network;
    }
}