using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.Web3;
using RedLockNet;
using StackExchange.Redis;
using Train.Solver.Core.Extensions;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using static Train.Solver.Core.Helpers.ResilientNodeHelper;
using static Train.Solver.Core.Blockchains.EVM.Helpers.EVMResilientNodeHelper;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services.Secret;
using Train.Solver.Core.Blockchains.EVM.Services;
using Temporalio.Activities;
using Train.Solver.Core.Blockchains.EVM.Models;
using Nethereum.RPC.Eth.DTOs;

namespace Train.Solver.Core.Blockchains.EVM.Activities;

public class EthereumEIP1559BlockchainActivities(
    SolverDbContext dbContext,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider)
    : EVMBlockchainActivitiesBase(dbContext, distributedLockFactory, cache, privateKeyProvider)
{
    public virtual double MaximumBaseFeeIncreasePerBlock => 0.125;

    public virtual int HighPriorityBlockCount => 5;

    protected override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
        => receipt.GasUsed.Value * receipt.EffectiveGasPrice;

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(EstimateFeeAsync)}")]
    public override async Task<Fee> EstimateFeeAsync(
        string networkName,
        EstimateFeeRequest request)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {network.Name} network", nameof(nodes));
        }

        var feeCurrency = network.Tokens.Single(x => x.TokenContract == null);

        var currency = network.Tokens.Single(x => x.Asset == request.Asset);

        var gasLimit = await GetGasLimitAsync(
            nodes,
            request.FromAddress,
            request.ToAddress,
            currency,
            request.Amount,
            request.CallData);

        if (network.GasLimitPercentageIncrease != null)
        {
            gasLimit = gasLimit.PercentageIncrease(network.GasLimitPercentageIncrease.Value);
        }

        var fee = await GetDataFromNodesAsync(nodes,
            async url => await GetFeeAmountAsync(new Web3(url), feeCurrency, gasLimit, HighPriorityBlockCount));

        return fee;

        async Task<Fee> GetFeeAmountAsync(
            IWeb3 web3,
            Token feeCurrency,
            BigInteger gasLimit,
            int blockCount)
        {
            var suggestedFees = await new MedianPriorityFeeHistorySuggestionStrategy(web3.Client).SuggestFeeAsync();
            var increasedBaseFee =
                suggestedFees.BaseFee.Value.CompoundInterestRate(MaximumBaseFeeIncreasePerBlock, blockCount);

            // Node returns 0 but transfer service throws exception in case of 0
            suggestedFees.MaxPriorityFeePerGas += 1;
            suggestedFees.MaxPriorityFeePerGas =
                suggestedFees.MaxPriorityFeePerGas.Value.PercentageIncrease(feeCurrency.Network.FeePercentageIncrease);

            return new Fee(
                feeCurrency.Asset,
                feeCurrency.Decimals,
                new EIP1559Data(suggestedFees.MaxPriorityFeePerGas.ToString(), increasedBaseFee.ToString(),
                    gasLimit.ToString()));
        }
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(IncreaseFee)}")]
    public override Fee IncreaseFee(Fee requestFee, int feeIncreasePercentage)
    {
        if (requestFee.Eip1559FeeData is null)
        {
            throw new ArgumentNullException(nameof(requestFee.Eip1559FeeData), "EIP1559 fee data is missing");
        }

        requestFee.Eip1559FeeData.MaxPriorityFeeInWei = BigInteger.Parse(requestFee.Eip1559FeeData.MaxPriorityFeeInWei)
            .PercentageIncrease(feeIncreasePercentage)
            .ToString();

        if (requestFee.Eip1559FeeData.L1FeeInWei != null)
        {
            requestFee.Eip1559FeeData.L1FeeInWei = BigInteger.Parse(requestFee.Eip1559FeeData.L1FeeInWei)
                .PercentageIncrease(feeIncreasePercentage)
                .ToString();
        }

        return requestFee;
    }

    #region Inherited Overrides

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetBatchTransactionAsync)}")]
    public override Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds)
    {
        return base.GetBatchTransactionAsync(networkName, transactionIds);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(ComposeSignedRawTransactionAsync)}")]
    public override Task<SignedTransaction> ComposeSignedRawTransactionAsync(string networkName, string fromAddress, string toAddress, string nonce, string amountInWei, string? callData, Fee fee)
    {
        return base.ComposeSignedRawTransactionAsync(networkName, fromAddress, toAddress, nonce, amountInWei, callData, fee);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetSpenderAllowanceAsync)}")]
    public override Task<decimal> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        return base.GetSpenderAllowanceAsync(networkName, ownerAddress, spenderAddress, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(PublishRawTransactionAsync)}")]
    public override Task<string> PublishRawTransactionAsync(string networkName, string fromAddress, SignedTransaction signedTransaction)
    {
        return base.PublishRawTransactionAsync(networkName, fromAddress, signedTransaction);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetSpenderAddressAsync)}")]
    public override Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        return base.GetSpenderAddressAsync(networkName, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(BuildTransactionAsync)}")]
    public override Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        return base.BuildTransactionAsync(networkName, transactionType, args);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(EnsureSufficientBalanceAsync)}")]
    public override Task EnsureSufficientBalanceAsync(string networkName, string address, string asset, decimal amount)
    {
        return base.EnsureSufficientBalanceAsync(networkName, address, asset, amount);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(FormatAddress)}")]
    public override string FormatAddress(string address)
    {
        return base.FormatAddress(address);
    }
    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GenerateAddressAsync)}")]
    public override Task<string> GenerateAddressAsync(string networkName)
    {
        return base.GenerateAddressAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetBalanceAsync)}")]
    public override Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        return base.GetBalanceAsync(networkName, address, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetEventsAsync)}")]
    public override Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        return base.GetEventsAsync(networkName, fromBlock, toBlock);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        return base.GetLastConfirmedBlockNumberAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request)
    {
        return base.ValidateAddLockSignatureAsync(networkName, request);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(ValidateAddress)}")]
    public override bool ValidateAddress(string address)
    {
        return base.ValidateAddress(address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumEip1559)}{nameof(GetNonceAsync)}")]
    public override Task<string> GetNonceAsync(string networkName, string address, string referenceId)
    {
        return base.GetNonceAsync(networkName, address, referenceId);
    }

    #endregion
}
