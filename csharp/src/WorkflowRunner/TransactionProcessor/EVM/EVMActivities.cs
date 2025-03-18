using Microsoft.EntityFrameworkCore;
using Temporalio.Activities;
using Train.Solver.Core.Blockchain.EVM;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Exceptions;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.EVM;

public class EVMActivities(
    SolverDbContext dbContext,
    IKeyedServiceProvider serviceProvider)
{
    [Activity]
    public virtual async Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        var currency = await dbContext.Tokens
            .Include(x => x.Network).ThenInclude(network => network.DeployedContracts)
            .SingleOrDefaultAsync(x =>
                 x.Asset.ToUpper() == asset.ToUpper()
                 && x.Network.Name.ToUpper() == networkName.ToUpper());

        if (currency == null)
        {
            throw new("Invalid currency");
        }

        return currency.IsNative ?
            currency.Network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : currency.Network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;
    }

    [Activity]
    public virtual async Task<Fee> IncreaseFeeAsync(
        string networkName,
        Fee fee)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IEVMBlockchainService>(network.Group);

        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var increasedFeeResult = blockchainService.IncreaseFee(fee, network.ReplacementFeePercentage);

        if (increasedFeeResult.IsFailed)
        {
            throw new($"Failed to increase fee. {increasedFeeResult.Errors.First().Message}");
        }

        return increasedFeeResult.Value;
    }

    [Activity]
    public virtual async Task<Core.Temporal.Abstractions.Models.TransactionModel> GetTransactionReceiptAsync(
        string networkName,
        IEnumerable<string> transactionIds)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName)
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IEVMBlockchainService>(network.Group);

        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        TransactionReceiptModel? receipt = null;

        foreach (var transactionId in transactionIds)
        {
            var receiptResult = await blockchainService.GetConfirmedTransactionAsync(networkName, transactionId);

            if (receiptResult.IsSuccess)
            {
                receipt = receiptResult.Value;
            }
            else if (receiptResult.HasError<TransactionFailedError>())
            {
                throw new TransactionFailedException("Transaction failed");
            }
        }

        if (receipt == null)
        {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        var transaction = new Core.Temporal.Abstractions.Models.TransactionModel
        {
            NetworkName = networkName,
            Status = TransactionStatus.Completed,
            TransactionHash = receipt.TransactionId,
            FeeAmount = receipt.FeeAmount,
            FeeAsset = receipt.FeeAsset,
            Timestamp = receipt.Timestamp != default
                ? DateTimeOffset.FromUnixTimeMilliseconds(receipt.Timestamp)
                : DateTimeOffset.UtcNow,
            Confirmations = receipt.Confirmations
        };

        return transaction;
    }

    [Activity]
    public async Task<SignedTransaction> ComposeSignedRawTransactionAsync(
        string networkName,
        string fromAddress,
        string toAddress,
        string nonce,
        string amountInWei,
        string? callData,
        Fee fee)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IEVMBlockchainService>(network.Group);

        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var result = await blockchainService.ComposeSignedRawTransactionAsync(
            networkName,
            fromAddress,
            toAddress,
            nonce,
            amountInWei,
            callData,
            fee);

        if (result.IsFailed)
        {
            throw new("Failed to compose raw transaction");
        }

        return result.Value;
    }

    [Activity]
    public async Task<string> PublishRawTransactionAsync(
        string networkName,
        string fromAddress,
        SignedTransaction signedTransaction)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IEVMBlockchainService>(network.Group);

        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        var result = await blockchainService.PublishRawTransactionAsync(networkName, fromAddress, signedTransaction);

        if (result.IsFailed)
        {
            if (result.HasError<TransactionUnderpricedError>())
            {
                throw new TransactionUnderpricedException("Transaction underprices");
            }
            else
            {
                throw new(result.Errors.First().Message);
            }
        }

        return result.Value;
    }
}
