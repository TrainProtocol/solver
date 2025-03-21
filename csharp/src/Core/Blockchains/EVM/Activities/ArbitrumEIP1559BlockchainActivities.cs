using System.Numerics;
using Nethereum.RPC.Eth.DTOs;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Blockchains.EVM.Models;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Transaction = Nethereum.RPC.Eth.DTOs.Transaction;

namespace Train.Solver.Core.Blockchains.EVM.Activities;

public class ArbitrumEIP1559BlockchainActivities(
    SolverDbContext dbContext,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider)
    : EthereumEIP1559BlockchainActivities(dbContext, distributedLockFactory, cache, privateKeyProvider)
{
    protected override BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceipt receipt)
        => receipt.GasUsed.Value * receipt.EffectiveGasPrice;

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(EstimateFeeAsync)}")]
    public async override Task<Fee> EstimateFeeAsync(
        string networkName, EstimateFeeRequest request)
    {
        var feeResult = await base.EstimateFeeAsync(networkName, request);

        feeResult.Eip1559FeeData.MaxPriorityFeeInWei = "1";

        return feeResult;
    }

    #region Inherited Overrides

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetNextNonceAsync)}")]
    public override Task<string> GetNextNonceAsync(string networkName, string address)
    {
        return base.GetNextNonceAsync(networkName, address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetBatchTransactionAsync)}")]
    public override Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds)
    {
        return base.GetBatchTransactionAsync(networkName, transactionIds);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(ComposeSignedRawTransactionAsync)}")]
    public override Task<SignedTransaction> ComposeSignedRawTransactionAsync(string networkName, string fromAddress, string toAddress, string nonce, string amountInWei, string? callData, Fee fee)
    {
        return base.ComposeSignedRawTransactionAsync(networkName, fromAddress, toAddress, nonce, amountInWei, callData, fee);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetSpenderAllowanceAsync)}")]
    public override Task<decimal> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        return base.GetSpenderAllowanceAsync(networkName, ownerAddress, spenderAddress, asset); 
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetSpenderAddressAsync)}")]
    public override Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        return base.GetSpenderAddressAsync(networkName, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(IncreaseFee)}")]
    public override Fee IncreaseFee(Fee requestFee, int feeIncreasePercentage)
    {
        return base.IncreaseFee(requestFee, feeIncreasePercentage);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(PublishRawTransactionAsync)}")]
    public override Task<string> PublishRawTransactionAsync(string networkName, string fromAddress, SignedTransaction signedTransaction)
    {
        return base.PublishRawTransactionAsync(networkName, fromAddress, signedTransaction);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(BuildTransactionAsync)}")]
    public override Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        return base.BuildTransactionAsync(networkName, transactionType, args);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(EnsureSufficientBalanceAsync)}")]
    public override Task EnsureSufficientBalanceAsync(string networkName, string address, string asset, decimal amount)
    {
        return base.EnsureSufficientBalanceAsync(networkName, address, asset, amount);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(FormatAddress)}")]
    public override string FormatAddress(string address)
    {
        return base.FormatAddress(address);
    }    

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GenerateAddressAsync)}")]
    public override Task<string> GenerateAddressAsync(string networkName)
    {
        return base.GenerateAddressAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetBalanceAsync)}")]
    public override Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        return base.GetBalanceAsync(networkName, address, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetEventsAsync)}")]
    public override Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        return base.GetEventsAsync(networkName, fromBlock, toBlock);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        return base.GetLastConfirmedBlockNumberAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request)
    {
        return base.ValidateAddLockSignatureAsync(networkName, request);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(ValidateAddress)}")]
    public override bool ValidateAddress(string address)
    {
        return base.ValidateAddress(address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMArbitrumEip1559)}{nameof(GetReservedNonceAsync)}")]
    public override Task<string> GetReservedNonceAsync(string networkName, string address, string referenceId)
    {
        return base.GetReservedNonceAsync(networkName, address, referenceId);
    }
    #endregion
}
