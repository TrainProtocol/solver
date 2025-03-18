using System.Reflection;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Services;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Exceptions;
using Train.Solver.WorkflowRunner.Models;
using Train.Solver.WorkflowRunner.TransactionProcessor.EVM;
using Train.Solver.WorkflowRunner.TransactionProcessor.Solana;
using Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Workflows;
using Train.Solver.WorkflowRunner.Workflows;
using TransactionModel = Train.Solver.Core.Temporal.Abstractions.Models.TransactionModel;

namespace Train.Solver.WorkflowRunner.Activities;

public class SwapActivities(
    SolverDbContext dbContext,
    RouteService routeService,
    ITemporalClient client,
    IKeyedServiceProvider serviceProvider)
{
    [Activity]
    public virtual async Task<string> CreateSwapAsync(
        HTLCCommitEventMessage commitEventMessage,
        decimal outputAmount,
        decimal feeAmount,
        string hashlock)
    {
        var sourceToken = await dbContext.Tokens
            .Include(x => x.Network)
            .SingleOrDefaultAsync(x =>
                x.Network.Name == commitEventMessage.SourceNetwork
                && x.Asset == commitEventMessage.SourceAsset);

        var destinationToken = await dbContext.Tokens
            .Include(x => x.Network)
            .SingleOrDefaultAsync(x =>
                x.Network.Name == commitEventMessage.DestinationNetwork
                && x.Asset == commitEventMessage.DestinationAsset);

        if (sourceToken == null || destinationToken == null)
        {
            throw new("Invalid source or destination token");
        }

        var sourceBlockchainService = serviceProvider.GetKeyedService<IBlockchainService>(sourceToken.Network.Group);

        if (sourceBlockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {sourceToken.Network.Group}");
        }

        var destinationBlockchainService = serviceProvider.GetKeyedService<IBlockchainService>(destinationToken.Network.Group);

        if (destinationBlockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {destinationToken.Network.Group}");
        }

        var swap = new Swap
        {
            Id = commitEventMessage.Id,
            SourceTokenId = sourceToken.Id,
            DestinationTokenId = destinationToken.Id,
            SourceAddress = sourceBlockchainService.FormatAddress(commitEventMessage.SenderAddress),
            DestinationAddress = destinationBlockchainService.FormatAddress(commitEventMessage.DestinationAddress),
            SourceAmount = commitEventMessage.Amount,
            DestinationAmount = outputAmount,
            Hashlock = hashlock,
            FeeAmount = feeAmount,
        };

        dbContext.Swaps.Add(swap);
        await dbContext.SaveChangesAsync();

        return swap.Id;
    }

    [Activity]
    public virtual async Task<Dictionary<string, string>> GetSolverAddressesAsync(params string[] networkNames)
    {
        var addresses = await dbContext.ManagedAccounts
            .Include(x => x.Network)
            .Where(x => networkNames.Contains(x.Network.Name) && x.Type == AccountType.LP)
            .ToDictionaryAsync(x => x.Network.Name, y => y.Address);

        if (networkNames.Count() != addresses.Count)
        {
            throw new Exception($"Faild to retrieve addresses");
        }

        return addresses;
    }

    [Activity]
    public virtual async Task<Guid> CreateSwapReferenceTransactionAsync(
        string networkName, string? swapId, TransactionType type)
    {
        var transaction = new Transaction
        {
            SwapId = swapId,
            NetworkName = networkName,
            Type = type,
            Status = TransactionStatus.Initiated,
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return transaction.Id;
    }

    [Activity]
    public virtual async Task UpdateSwapReferenceTransactionAsync(Guid id, TransactionModel confirmedTransaction)
    {
        var transaction = await dbContext.Transactions
            .SingleOrDefaultAsync(x => x.Id == id);

        if (transaction == null)
        {
            throw new($"Transaction with id {id} not found.");
        }

        transaction.TransactionId = confirmedTransaction.TransactionHash;
        transaction.Status = TransactionStatus.Completed;
        transaction.Confirmations = confirmedTransaction.Confirmations;
        transaction.Timestamp = confirmedTransaction.Timestamp;
        transaction.FeeAmount = confirmedTransaction.FeeAmount;
        transaction.FeeAsset = confirmedTransaction.FeeAsset;
        transaction.Amount = confirmedTransaction.Amount;
        transaction.Asset = confirmedTransaction.Asset;
        transaction.NetworkName = confirmedTransaction.NetworkName;

        var token = await dbContext.Tokens
            .Include(x => x.TokenPrice)
            .SingleOrDefaultAsync(x =>
                x.Asset == confirmedTransaction.Asset
                && x.Network.Name == confirmedTransaction.NetworkName);

        if (token == null)
        {
            throw new($"Token with asset {confirmedTransaction.Asset} not found.");
        }

        transaction.UsdPrice = token.TokenPrice.PriceInUsd;

        var feeToken = await dbContext.Tokens
            .Include(x => x.TokenPrice)
            .SingleOrDefaultAsync(x =>
                x.Asset == confirmedTransaction.FeeAsset
                && x.Network.Name == confirmedTransaction.NetworkName);

        if (feeToken == null)
        {
            throw new($"Token with asset {confirmedTransaction.FeeAsset} not found.");
        }

        transaction.FeeUsdPrice = feeToken.TokenPrice.PriceInUsd;

        await dbContext.SaveChangesAsync();
    }

    [Activity]
    public virtual async Task<string> SelectTransactionProcessorAsync(string networkName)
    {
        var network = await dbContext.Networks
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new("Invalid network");
        }

        Type type = network.Group switch
        {
            // Networks starting with EVM
            NetworkGroup.EVM_LEGACY or
            NetworkGroup.EVM_EIP1559 or
            NetworkGroup.EVM_ARBITRUM_LEGACY or
            NetworkGroup.EVM_ARBITRUM_EIP1559 or
            NetworkGroup.EVM_OPTIMISM_EIP1559 or
            NetworkGroup.EVM_OPTIMISM_LEGACY or
            NetworkGroup.EVM_POLYGON_LEGACY or
            NetworkGroup.EVM_POLYGON_EIP1559 => typeof(EVMTransactionProcessor),

            // Networks starting with STARKNET
            NetworkGroup.STARKNET => typeof(StarknetTransactionProcessor),

            // Solana-specific network
            NetworkGroup.SOLANA => typeof(SolanaTransactionProcessor),

            // Default for unimplemented networks
            _ => throw new NotImplementedException($"No processor implemented for {network.Group}")
        };

        var workflowAttribute = type.GetCustomAttribute<WorkflowAttribute>();

        if (workflowAttribute == null)
        {
            throw new InvalidOperationException($"Type {type.Name} does not have a Workflow attribute.");
        }

        return string.IsNullOrEmpty(workflowAttribute.Name)
            ? type.Name
            : workflowAttribute.Name;
    }

    [Activity]
    public async Task<string> StartSwapWorkflowAsync(HTLCCommitEventMessage signal)
    {
        if (signal == null)
        {
            throw new ArgumentNullException(nameof(signal), "Signal cannot be null.");
        }

        try
        {
            var existingSwap = await dbContext.Swaps
                .SingleOrDefaultAsync(x => x.Id == signal.Id);

            if (existingSwap != null)
            {
                return existingSwap.Id;
            }

            var workflowHandle = await client.StartWorkflowAsync<SwapWorkflow>(
                workflow => workflow.RunAsync(signal),
                new WorkflowOptions
                {
                    Id = $"{signal.Id}",
                    TaskQueue = Constants.CSharpTaskQueue,
                });

            return workflowHandle.Id;
        }
        catch (WorkflowAlreadyStartedException)
        {
            return signal.Id;
        }
    }

    [Activity]
    public virtual async Task<LimitModel> GetLimitAsync(SourceDestinationRequest request)
    {
        var limitResult = await routeService.GetLimitAsync(request);

        if (limitResult.IsFailed)
        {
            if (limitResult.HasError<RouteNotFoundError>())
            {
                throw new RouteNotFoundException("Route not found");
            }

            throw new(limitResult.Errors.First().Message);
        }

        return limitResult.Value;
    }

    [Activity]
    public virtual async Task<QuoteModel> GetQuoteAsync(QuoteRequest request)
    {
        var quoteResult = await routeService.GetQuoteAsync(request);

        if (quoteResult.IsFailed)
        {
            throw new(quoteResult.Errors.First().Message);
        }

        return quoteResult.Value;
    }

    [Activity]
    public virtual async Task<HashlockModel> GenerateHashlockAsync()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var secretHex = bytes.ToHex(prefix: true);

        var secret = new HexBigInteger(secretHex).Value.ToString();

        var firstHash = SHA256.HashData(bytes);

        var hashHex = firstHash.ToHex(prefix: true);

        return await Task.FromResult(new HashlockModel(secret, hashHex));
    }

    [Activity]
    public virtual async Task UpdateTransactionComplitionDetailsAsync(
        string networkName,
        string feeAsset,
        decimal currentFee,
        string callDataAsset,
        TransactionType callDataType)
    {
        var feeCurrency = await dbContext.Tokens
            .SingleOrDefaultAsync(x => x.Asset.ToUpper() == feeAsset.ToUpper()
                && x.Network.Name.ToUpper() == networkName.ToUpper());

        if (feeCurrency is null)
        {
            return;
        }

        var callDataCurrency = callDataAsset == feeAsset
            ? feeCurrency
            : await dbContext.Tokens.SingleOrDefaultAsync(
                x => x.Asset.ToUpper() == callDataAsset.ToUpper()
                && x.Network.Name.ToUpper() == networkName.ToUpper());

        if (callDataCurrency is null)
        {
            return;
        }

        var detail = await dbContext.Expenses
            .SingleOrDefaultAsync(x =>
                x.TokenId == callDataCurrency.Id
                && x.TransactionType == callDataType
                && x.FeeTokenId == feeCurrency.Id);

        if (detail == null)
        {
            detail = new()
            {
                TokenId = callDataCurrency.Id,
                FeeTokenId = feeCurrency.Id,
                TransactionType = callDataType
            };

            dbContext.Expenses.Add(detail);
        }

        detail.AddFeeValue(currentFee);

        await dbContext.SaveChangesAsync();
    }

    [Activity]
    public IEnumerable<BlockRange> GenerateBlockRanges(ulong start, ulong end, uint chunkSize)
    {
        if (chunkSize == 0)
            throw new ArgumentException("Max size must be greater than 0", nameof(chunkSize));

        var result = new List<BlockRange>();

        ulong currentStart = start;

        while (currentStart <= end)
        {
            ulong currentEnd = Math.Min(currentStart + chunkSize - 1, end);
            result.Add(new BlockRange(currentStart, currentEnd));
            currentStart = currentEnd + 1;
        }

        return result;
    }
    [Activity]
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
            .Select(s=>s.Id)
            .ToListAsync();
    }
}
