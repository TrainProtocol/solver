using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;

public class EFSwapRepository(INetworkRepository networkRepository, SolverDbContext dbContext) : ISwapRepository
{
    public async Task<Swap> CreateAsync(string id, string senderAddress, string destinationAddress, string sourceNetworkName, string sourceAsset, decimal sourceAmount, string destinationNetworkName, string destinationAsset, decimal destinationAmount, string hashlock, decimal feeAmount)
    {

        var sourceToken = await networkRepository.GetTokenAsync(sourceNetworkName, sourceAsset);
        var destinationToken = await networkRepository.GetTokenAsync(destinationNetworkName, destinationAsset);

        if (sourceToken == null || destinationToken == null)
        {
            throw new("Invalid source or destination token");
        }

        var swap = new Swap
        {
            Id = id,
            SourceTokenId = sourceToken.Id,
            DestinationTokenId = destinationToken.Id,
            SourceAddress = senderAddress,
            DestinationAddress = destinationAddress,
            SourceAmount = sourceAmount,
            SourceTokenPrice = sourceToken.TokenPrice.PriceInUsd,
            DestinationAmount = destinationAmount,
            DestinationTokenPrice = destinationToken.TokenPrice.PriceInUsd,
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

    public async Task<Swap?> GetAsync(string id)
    {
        return await dbContext.Swaps
            .Include(x => x.Transactions)
            .Include(x => x.SourceToken.Network)
            .Include(x => x.SourceToken.TokenPrice)
            .Include(x => x.DestinationToken.Network)
            .Include(x => x.DestinationToken.TokenPrice)
            .FirstOrDefaultAsync(x => x.Id == id);
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
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<Transaction> InitiateSwapTransactionAsync(string networkName, string swapId, TransactionType transactionType)
    {
        var transaction = new Transaction
        {
            SwapId = swapId,
            NetworkName = networkName,
            Type = transactionType,
            Status = TransactionStatus.Initiated,
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return transaction;
    }

    public async Task<Guid> CreateSwapTransactionAsync(
        string networkName,
        string swapId,
        TransactionType transactionType,
        string transactionHash,
        string asset,
        decimal amount,
        int confirmations,
        DateTimeOffset timestamp,
        string feeAsset,
        decimal feeAmount)
    {
        var token = await networkRepository.GetTokenAsync(networkName, asset);
       
        if (token == null)
        {
            throw new($"Token with asset {asset} not found.");
        }

        var feeToken = await networkRepository.GetTokenAsync(networkName, feeAsset);

        if (feeToken == null)
        {
            throw new($"Token with asset {feeAsset} not found.");
        }

        var transaction = new Transaction
        {
            TransactionId = transactionHash,
            Status = TransactionStatus.Completed,
            Confirmations = confirmations,
            Timestamp = timestamp,
            FeeAmount = feeAmount,
            FeeAsset = feeToken.Asset,
            Amount = amount,
            Asset = token.Asset,
            FeeUsdPrice = feeToken.TokenPrice.PriceInUsd,
            NetworkName = networkName,
            SwapId = swapId,
            Type = transactionType,
            UsdPrice = token.TokenPrice.PriceInUsd
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return transaction.Id;
    }
}