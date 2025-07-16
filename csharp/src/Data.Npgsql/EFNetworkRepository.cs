using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;

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

    public async Task<Network?> CreateAsync(
       string networkName,
       string displayName,
       NetworkType type,
       TransactionFeeType feeType,
       string chainId,
       int feePercentageIncrease,
       string htlcNativeContractAddress,
       string htlcTokenContractAddress,
       string nativeTokenSymbol,
       string nativeTokenContract,
       int nativeTokenDecimals)
    {
        var networkExists = await dbContext.Networks.AnyAsync(x => x.Name == networkName);

        if (networkExists)
            return null;

        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
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

            var token = new Token
            {
                Asset = nativeTokenSymbol,
                Decimals = nativeTokenDecimals,
                TokenContract = nativeTokenContract,
            };

            network.NativeToken = token;
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
            return network;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Node?> CreateNodeAsync(string networkName, string url)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            return null;
        }

        var node = new Node { Url = url };
        network.Nodes.Add(node);
        await dbContext.SaveChangesAsync();

        return node;
    }

    public async Task<Token?> CreateTokenAsync(string networkName, string symbol, string? contract, int decimals)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            return null;
        }

        if (network.Tokens.Any(x => x.Asset == symbol && x.TokenContract == contract))
        {
            return null;
        }

        var token = new Token
        {
            Asset = symbol,
            Decimals = decimals,
            TokenContract = contract,
        };

        network.Tokens.Add(token);
        await dbContext.SaveChangesAsync();

        return token;
    }

    public async Task<Token?> CreateNativeTokenAsync(string networkName, string symbol, int decimals)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            return null;
        }

        if (network.NativeTokenId != null)
        {
            return null;
        }

        var token = new Token
        {
            Asset = symbol,
            Decimals = decimals,
            NetworkId = network.Id,
        };

        network.NativeToken = token;
        await dbContext.SaveChangesAsync();

        return token;
    }

    public async Task DeleteTokenAsync(string networkName, string symbol)
    {
        var tokenToDelete = await GetTokenAsync(networkName, symbol);

        if (tokenToDelete != null)
        {
            dbContext.Tokens.Remove(tokenToDelete);
            await dbContext.SaveChangesAsync();
        }
    }
}