using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Serilog;
using Temporalio.Activities;
using Temporalio.Client;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Exceptions;
using Train.Solver.WorkflowRunner.Models;
using Train.Solver.WorkflowRunner.Workflows;

namespace Train.Solver.WorkflowRunner.Activities;

public class BlockchainActivities(
    SolverDbContext dbContext,
    IKeyedServiceProvider serviceProvider,
    ITemporalClient temporalClient)
{
    [Activity]
    public virtual async Task<string> GetReservedNonceAsync(
        string networkName,
        string address,
        string referenceId)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);

        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var reservedNonceResult = await blockchainService.GetReservedNonceAsync(
            networkName,
            address,
            referenceId);

        if (reservedNonceResult.IsFailed)
        {
            throw new($"Failed to get reserved nonce. {reservedNonceResult.Errors.First().Message}");
        }

        return reservedNonceResult.Value;
    }


    [Activity]
    public virtual async Task<decimal> GetBalanceAsync(string networkName, string asset, string address)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }


        var balanceResult = await blockchainService.GetBalanceAsync(networkName, address, asset);

        if (balanceResult.IsFailed)
        {
            throw new($"Failed to get balance. {balanceResult.Errors.First().Message}");
        }

        return balanceResult.Value.Amount;
    }

    [Activity]
    public virtual async Task EnsureSufficientBalanceAsync(
        string networkName,
        string asset,
        string address,
        decimal amount)
    {
        var balance = await GetBalanceAsync(networkName, asset, address);

        if (amount > balance)
        {
            Log.Warning($"Insufficient {asset} funds in {networkName} {address}{{alert}}", AlertChannel.AtomicPrimary);
            throw new($"Insufficient balance on {address}. Balance is less than {amount}");
        }
    }

    [Activity]
    public virtual async Task<PrepareTransactionResponse> PrepareTransactionAsync(
        string networkName, TransactionType type, string args)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var result = await blockchainService.BuildTransactionAsync(networkName, type, args);

        if (result.IsFailed)
        {
            throw new($"Failed to prepare transaction. {result.Errors.First().Message}");
        }

        return result.Value;
    }

    [Activity]
    public virtual async Task<Fee> EstimateFeesAsync(
        string networkName,
        EstimateFeeRequest request,
        string? correlationId)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        // Todo
        //if (!string.IsNullOrEmpty(request.CallData)
        //    && callDataFormatterResolver.TryGetValue(network.Group, out var callDataFormatter))
        //{
        //    request.CallData = callDataFormatter.Format(request.CallData);
        //}

        var result = await blockchainService.EstimateFeeAsync(
            networkName,
            request.Asset,
            request.FromAddress,
            request.ToAddress,
            request.Amount,
            request.CallData);

        if (result.IsFailed)
        {
            if (result.HasError<InsuficientFundsForGasLimitError>())
            {
                Log.Error($"Insufficient {request.Asset} funds in {networkName} {request.FromAddress}{{alert}}", AlertChannel.AtomicPrimary);
            }
            else if (result.HasError<InvalidTimelockError>())
            {
                throw new InvalidTimelockException("Invalid timelock");
            }
            else if (result.HasError<HashlockAlreadySetError>())
            {
                throw new HashlockAlreadySetException("Hashlock already set");
            }
            else if (result.HasError<HTLCAlreadyExistsError>())
            {
                throw new HTLCAlreadyExistsException("HTLC already exists");
            }
            else if (result.HasError<AlreadyClaimedError>())
            {
                throw new AlreadyClaimedExceptions("HTLC already cleamed");
            }

            throw new($"Failed to estimate fee. {result.Errors.First().Message}");
        }

        return result.Value;
    }

    [Activity]
    public virtual async Task<decimal> GetAllowanceAsync(
        string networkName,
        string asset,
        string ownerAddress,
        string spenderAddress)
    {
        var network = await dbContext.Networks
          .Where(x => x.Name.ToUpper() == networkName.ToUpper())
          .Include(x => x.Tokens)
          .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var token = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == asset.ToUpper());

        if (token == null)
        {
            throw new("Invalid token");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }
        var result = await blockchainService.GetSpenderAllowanceAsync(
            networkName, ownerAddress, spenderAddress, asset);

        if (result.IsFailed)
        {
            throw new($"Failed to get allowance. {result.Errors.First().Message}");
        }

        return Web3.Convert.FromWei(BigInteger.Parse(result.Value), token.Decimals);
    }

    [Activity]
    public async Task<HTLCBlockEvent> GetEventsAsync(
      string networkName,
      ulong fromBlock,
      ulong toBlock)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new($"Network: {networkName.ToUpper()} is not registered");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var htlcBlockEventResult = await blockchainService.GetEventsAsync(networkName, fromBlock, toBlock);

        if (htlcBlockEventResult.IsFailed)
        {
            throw new(htlcBlockEventResult.Errors.First().Message);
        }

        return htlcBlockEventResult.Value;
    }

    [Activity]
    public virtual async Task<BlockNumberWithHash> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
           .Where(x => x.Name.ToUpper() == networkName.ToUpper())
           .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new($"Network: {networkName.ToUpper()} is not registered");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var result = await blockchainService.GetLastConfirmedBlockNumberAsync(networkName);

        if (result.IsFailed)
        {
            throw new(result.Errors.First().Message);
        }

        return new(ulong.Parse(result.Value.BlockNumber), result.Value.BlockHash!);
    }

    [Activity]
    public async Task RefundAsync(string swapId)
    {
        var swap = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .Include(x => x.DestinationToken.Network)
                .ThenInclude(network => network.ManagedAccounts)
            .SingleAsync(s => s.Id == swapId);

        await temporalClient.StartWorkflowAsync(
            (TransactionWorkflow x) => x.ExecuteTransactionAsync(
                new()
                {
                    PrepareArgs = new HTLCRefundTransactionPrepareRequest
                    {
                        Id = swap.Id,
                        Asset = swap.DestinationToken.Asset,
                    }.ToArgs(),
                    Type = TransactionType.HTLCRefund,
                    CorrelationId = swap.Id,
                    NetworkName = swap.DestinationToken.Network.Name,
                    FromAddress = swap.DestinationToken.Network.ManagedAccounts.First().Address,
                }, swap.Id),
            new(id: TransactionWorkflow.BuildId(swap.DestinationToken.Network.Name, TransactionType.HTLCRefund), taskQueue: Constants.CSharpTaskQueue)
            {
                IdReusePolicy = Temporalio.Api.Enums.V1.WorkflowIdReusePolicy.TerminateIfRunning,
            });
    }
}
