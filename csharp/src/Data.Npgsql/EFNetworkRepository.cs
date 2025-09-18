using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Npgsql;

public class EFNetworkRepository(
    ITokenPriceRepository tokenPriceRepository,
    SolverDbContext dbContext) : INetworkRepository
{

    public async Task<Network?> GetAsync(string networkName)
    {
        return await dbContext.Networks
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.Nodes)
            .FirstOrDefaultAsync(x => x.Name == networkName);
    }

    public async Task<IEnumerable<Network>> GetAllAsync(NetworkType[]? types)
    {
        return await dbContext.Networks
            .Where(x => types == null || types.Contains(x.Type))
            .Include(x => x.Tokens)
            .ThenInclude(x => x.TokenPrice)
            .Include(x => x.Nodes)
            .ToListAsync();
    }

    public async Task<Network?> CreateAsync(CreateNetworkRequest request)
    {
        var networkExists = await dbContext.Networks.AnyAsync(x => x.Name == request.NetworkName);

        if (networkExists)
            return null;

        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var network = new Network
            {
                Name = request.NetworkName,
                ChainId = request.ChainId,
                DisplayName = request.DisplayName,
                FeePercentageIncrease = request.FeePercentageIncrease,
                FeeType = request.FeeType,
                HTLCNativeContractAddress = request.HtlcNativeContractAddress,
                HTLCTokenContractAddress = request.HtlcTokenContractAddress,
                Type = request.Type,
            };

            dbContext.Networks.Add(network);
            await dbContext.SaveChangesAsync();

            var tokenPrice = await tokenPriceRepository.GetAsync(request.NativeTokenPriceSymbol);

            if (tokenPrice == null)
            {
                throw new Exception($"Token price for '{request.NativeTokenPriceSymbol}' not found.");
            }

            var token = new Token
            {
                Asset = request.NativeTokenSymbol,
                Decimals = request.NativeTokenDecimals,
                TokenContract = request.NativeTokenContract,
                NetworkId = network.Id,
                TokenPriceId = tokenPrice.Id,
            };

            network.NativeToken = token;
            await dbContext.SaveChangesAsync();

            var node = new Node
            {
                ProviderName = request.NodeProvider,
                Url = request.NodeUrl,
            };

            network.Nodes.Add(node);
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

    public async Task<Network?> UpdateAsync(
       string networkName,
       UpdateNetworkRequest request)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            throw new Exception("Network not found");
        }

        network.DisplayName = request.DisplayName;
        network.FeeType = request.FeeType;
        network.FeePercentageIncrease = request.FeePercentageIncrease;
        network.HTLCNativeContractAddress = request.HtlcNativeContractAddress;
        network.HTLCTokenContractAddress = request.HtlcTokenContractAddress;

        await dbContext.SaveChangesAsync();

        return network;
    }

    public async Task<Node?> CreateNodeAsync(
        string networkName,
        CreateNodeRequest request)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            return null;
        }

        var node = new Node { Url = request.Url, ProviderName = request.ProviderName, NetworkId = network.Id };
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

    public async Task<Token?> CreateTokenAsync(
        string networkName,
        CreateTokenRequest request)
    {
        var network = await GetAsync(networkName);

        if (network == null)
        {
            return null;
        }

        if (network.Tokens.Any(x => x.Asset == request.Symbol && x.TokenContract == request.Contract))
        {
            return null;
        }

        var tokenPrice = await tokenPriceRepository.GetAsync(request.PriceSymbol);

        if (tokenPrice == null)
        {
            throw new Exception($"Token price for '{request.PriceSymbol}' not found.");
        }

        var token = new Token
        {
            Asset = request.Symbol,
            Decimals = request.Decimals,
            TokenContract = request.Contract,
            TokenPriceId = tokenPrice.Id,
            NetworkId = network.Id,
        };

        dbContext.Tokens.Add(token);
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