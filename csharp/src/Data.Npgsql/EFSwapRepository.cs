using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFSwapRepository(INetworkRepository networkRepository, SolverDbContext dbContext) : ISwapRepository
{
    public async Task<Swap> CreateAsync(
        string commitId,
        string senderAddress,
        string destinationAddress,
        string sourceNetworkName,
        string sourceAsset,
        string sourceAmount,
        string destinationNetworkName,
        string destinationAsset,
        string destinationAmount,
        string hashlock,
        string feeAmount)
    {

        var sourceToken = await networkRepository.GetTokenAsync(sourceNetworkName, sourceAsset);
        var destinationToken = await networkRepository.GetTokenAsync(destinationNetworkName, destinationAsset);

        if (sourceToken == null || destinationToken == null)
        {
            throw new("Invalid source or destination token");
        }

        var swap = new Swap
        {
            CommitId = commitId,
            SourceTokenId = sourceToken.Id,
            DestinationTokenId = destinationToken.Id,
            SourceAddress = senderAddress,
            DestinationAddress = destinationAddress,
            SourceAmount = sourceAmount,
            DestinationAmount = destinationAmount,
            Hashlock = hashlock,
            FeeAmount = feeAmount,
        };

        dbContext.Swaps.Add(swap);
        await dbContext.SaveChangesAsync();

        return swap;
    }

    public async Task<List<Swap>> GetAllAsync(uint page = 1, uint size = 20, string[]? addresses = null)
    {
        return await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network)
            .Include(x => x.DestinationToken.TokenPrice)
            .Include(x => x.Transactions)
            .Where(x => addresses == null
                || addresses.Contains(x.SourceAddress.ToLower())
                || addresses.Contains(x.DestinationAddress.ToLower()))
            .OrderByDescending(x => x.CreatedDate)
            .Skip((int)(page * size))
            .Take((int)size)
            .ToListAsync();
    }

    public async Task<Swap?> GetAsync(string commitId)
    {
        return await dbContext.Swaps
            .Include(x => x.Transactions)
            .Include(x => x.SourceToken.Network)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network)
            .Include(x => x.DestinationToken.TokenPrice)
            .FirstOrDefaultAsync(x => x.CommitId == commitId);
    }

    public async Task<List<string>> GetNonRefundedSwapIdsAsync()
    {
        return await dbContext.Swaps
            .Where(x =>
                x.Transactions.All(t => t.Type != TransactionType.HTLCRedeem)
                &&
                x.Transactions.All(t => t.Type != TransactionType.HTLCRefund)
                &&
                x.Transactions.Any(t => t.Type == TransactionType.HTLCLock)
            )
            .Select(s => s.CommitId)
            .ToListAsync();
    }

    public async Task<int> CreateSwapTransactionAsync(
        string networkName,
        string swapId,
        TransactionType transactionType,
        string transactionHash,
        string asset,
        string amount,
        int confirmations,
        DateTimeOffset timestamp,
        string feeAsset,
        string feeAmount)
    {
        var network = await networkRepository.GetAsync(networkName);

        if (network == null)
        {
            throw new($"Network {networkName} not found.");
        }

        var token = network.Tokens.FirstOrDefault(x => x.Asset == asset);

        if (token == null)
        {
            throw new($"Token with asset {asset} not found.");
        }

        var transaction = new Transaction
        {
            TransactionHash = transactionHash,
            Status = TransactionStatus.Completed,
            Confirmations = confirmations,
            Timestamp = timestamp,
            FeeAmount = feeAmount,
            FeeTokenId = network.NativeToken!.Id,
            Amount = amount,
            TokenId = token.Id,
            NetworkName = networkName,
            SwapId = swapId,
            Type = transactionType,
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return transaction.Id;
    }
}