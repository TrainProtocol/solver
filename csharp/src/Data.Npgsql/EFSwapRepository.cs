using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Npgsql;

public class EFSwapRepository(
    INetworkRepository networkRepository,
    IRouteRepository routeRepository,
    SolverDbContext dbContext) : ISwapRepository
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
        var route = await routeRepository.GetAsync(
            sourceNetworkName,
            sourceAsset,
            destinationNetworkName,
            destinationAsset,
            null);

        if (route == null)
        {
            throw new("Invalid route");
        }

        var swap = new Swap
        {
            CommitId = commitId,
            RouteId = route.Id,
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

    public async Task<List<Swap>> GetAllAsync(uint page = 1, uint size = 15)
    {
        return await GetBaseQuery()
            .OrderByDescending(x => x.CreatedDate)
            .Skip((int)(page * size))
            .Take((int)size)
            .ToListAsync();
    }

    public async Task<Swap?> GetAsync(string commitId)
    {
        return await GetBaseQuery().FirstOrDefaultAsync(x => x.CommitId == commitId);
    }

    public async Task<List<string>> GetNonRefundedSwapsAsync()
    {
        return await dbContext.Swaps
            .Where(x =>
                x.Transactions.All(t => t.Type != TransactionType.HTLCRedeem)
                &&
                x.Transactions.All(t => t.Type != TransactionType.HTLCRefund)
                &&
                x.Transactions.Any(t => t.Type == TransactionType.HTLCLock))
            .Select(x => x.CommitId)
            .ToListAsync();
    }

    public async Task<int> CreateSwapTransactionAsync(
        string networkName,
        int? swapId,
        TransactionType transactionType,
        string transactionHash,
        DateTimeOffset timestamp,
        string feeAmount)
    {
        var network = await networkRepository.GetAsync(networkName);

        if (network == null)
        {
            throw new($"Network {networkName} not found.");
        }

        var transaction = new Transaction
        {
            TransactionHash = transactionHash,
            Status = TransactionStatus.Completed,
            Timestamp = timestamp,
            FeeAmount = feeAmount,
            NetworkId = network.Id,
            SwapId = swapId,
            Type = transactionType,
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return transaction.Id;
    }

    public async Task<int> CreateSwapMetricAsync(
        int swapId,
        string sourceNetwork,
        string sourceToken,
        string destinationNetwork,
        string destinationToken,
        decimal volumeInUsd,
        decimal profitInUsd)
    {
        var swapMetric = new SwapMetric
        {
            SwapId = swapId,
            SourceNetwork = sourceNetwork,
            SourceToken = sourceToken,
            DestinationNetwork = destinationNetwork,
            DestinationToken = destinationToken,
            VolumeInUsd = volumeInUsd,
            ProfitInUsd = profitInUsd
        };  

        dbContext.SwapMetrics.Add(swapMetric);
        await dbContext.SaveChangesAsync();

        return swapMetric.Id;
    }

    private IQueryable<Swap> GetBaseQuery()
      => dbContext.Swaps
            .Include(x => x.Transactions).ThenInclude(x => x.Network.NativeToken!.TokenPrice)
            .Include(x => x.Route.SourceWallet.SignerAgent)
            .Include(x => x.Route.SourceToken.Network)
            .Include(x => x.Route.SourceToken.TokenPrice)
            .Include(x => x.Route.DestinationWallet.SignerAgent)
            .Include(x => x.Route.DestinationToken.Network)
            .Include(x => x.Route.DestinationToken.TokenPrice);
}
