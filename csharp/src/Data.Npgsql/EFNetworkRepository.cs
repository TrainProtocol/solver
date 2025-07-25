using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Npgsql;

public class EFNetworkRepository(SolverDbContext dbContext) : INetworkRepository
{
   
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

    public async Task<Node?> CreateNodeAsync(
        string networkName,
        string providerName, 
        string url)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            return null;
        }

        var node = new Node { Url = url, ProviderName = providerName, NetworkId = network.Id };
        network.Nodes.Add(node);
        await dbContext.SaveChangesAsync();

        return node;
    }

    public async Task DeleteNodeAsync(string networkName, string providerName)
    {
        await dbContext.Nodes
            .Where(x => x.Network.Name == networkName && x.ProviderName == providerName)
            .ExecuteDeleteAsync();
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
        await dbContext.Tokens
            .Where(x => x.Network.Name == networkName && x.Asset == symbol)
            .ExecuteDeleteAsync();
    }

    public Task<Token?> GetTokenAsync(string networkName, string symbol)
    {
        return dbContext.Tokens
            .Include(x => x.TokenPrice)
            .FirstOrDefaultAsync(x => x.Network.Name == networkName && x.Asset == symbol);
    }
}