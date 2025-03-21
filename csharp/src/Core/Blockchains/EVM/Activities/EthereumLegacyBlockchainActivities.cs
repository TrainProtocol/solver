using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Nethereum.Util;
using Nethereum.Web3;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Blockchains.EVM.Models;
using Train.Solver.Core.Data;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services.Secret;
using static Train.Solver.Core.Blockchains.EVM.Helpers.EVMResilientNodeHelper;

namespace Train.Solver.Core.Blockchains.EVM.Activities;

public class EthereumLegacyBlockchainActivities(
    SolverDbContext dbContext,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) :
    EVMBlockchainActivitiesBase(dbContext, distributedLockFactory, cache, privateKeyProvider)
{
    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(EstimateFeeAsync)}")]
    public override async Task<Fee> EstimateFeeAsync(
            string networkName,
            EstimateFeeRequest request)
    {
        var resultMap = new Dictionary<string, Fee>();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {networkName} network");
        }

        var feeCurrency = network.Tokens.Single(x => x.TokenContract == null);

        var currency = network.Tokens.Single(x => x.Asset == request.Asset);

        var gasLimitResult = await
            GetGasLimitAsync(nodes,
                request.FromAddress,
                request.ToAddress,
                currency,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(network.FeePercentageIncrease);

        if (network.FixedGasPriceInGwei != null &&
            BigInteger.TryParse(network.FixedGasPriceInGwei, out var fixedGasPriceInGwei))
        {
            var fixedGasPriceInWei = Web3.Convert.ToWei(fixedGasPriceInGwei, UnitConversion.EthUnit.Gwei);
            gasPrice = BigInteger.Max(fixedGasPriceInWei, currentGasPriceResult.Value.PercentageIncrease(20));
        }

        return new Fee(
            feeCurrency.Asset,
            feeCurrency.Decimals,
            new LegacyData(gasPrice.ToString(), gasLimitResult.ToString()));

    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(IncreaseFee)}")]
    public override Fee IncreaseFee(
        Fee requestFee,
        int feeIncreasePercentage)
    {
        if (requestFee.LegacyFeeData is null)
        {
            throw new ArgumentNullException(nameof(requestFee.LegacyFeeData), "Legacy fee data is missing");
        }

        requestFee.LegacyFeeData.GasPriceInWei = BigInteger.Parse(requestFee.LegacyFeeData.GasPriceInWei)
                        .PercentageIncrease(feeIncreasePercentage)
                        .ToString();

        if (requestFee.LegacyFeeData?.L1FeeInWei != null)
        {
            requestFee.LegacyFeeData.L1FeeInWei = BigInteger.Parse(requestFee.LegacyFeeData.L1FeeInWei)
                .PercentageIncrease(feeIncreasePercentage)
                .ToString();
        }

        return requestFee;
    }

    #region Inherited Overrides

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetNextNonceAsync)}")]
    public override Task<string> GetNextNonceAsync(string networkName, string address)
    {
        return base.GetNextNonceAsync(networkName, address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetBatchTransactionAsync)}")]
    public override Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds)
    {
        return base.GetBatchTransactionAsync(networkName, transactionIds);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(ComposeSignedRawTransactionAsync)}")]
    public override Task<SignedTransaction> ComposeSignedRawTransactionAsync(string networkName, string fromAddress, string toAddress, string nonce, string amountInWei, string? callData, Fee fee)
    {
        return base.ComposeSignedRawTransactionAsync(networkName, fromAddress, toAddress, nonce, amountInWei, callData, fee);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetSpenderAllowanceAsync)}")]
    public override Task<decimal> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        return base.GetSpenderAllowanceAsync(networkName, ownerAddress, spenderAddress, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request)
    {
        return base.ValidateAddLockSignatureAsync(networkName, request);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(PublishRawTransactionAsync)}")]
    public override Task<string> PublishRawTransactionAsync(string networkName, string fromAddress, SignedTransaction signedTransaction)
    {
        return base.PublishRawTransactionAsync(networkName, fromAddress, signedTransaction);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetSpenderAddressAsync)}")]
    public override Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        return base.GetSpenderAddressAsync(networkName, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetReservedNonceAsync)}")]
    public override Task<string> GetReservedNonceAsync(string networkName, string address, string referenceId)
    {
        return base.GetReservedNonceAsync(networkName, address, referenceId);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        return base.GetLastConfirmedBlockNumberAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(ValidateAddress)}")]
    public override bool ValidateAddress(string address)
    {
        return base.ValidateAddress(address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetEventsAsync)}")]
    public override Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        return base.GetEventsAsync(networkName, fromBlock, toBlock);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GetBalanceAsync)}")]
    public override Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        return base.GetBalanceAsync(networkName, address, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(GenerateAddressAsync)}")]
    public override Task<string> GenerateAddressAsync(string networkName)
    {
        return base.GenerateAddressAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(FormatAddress)}")]
    public override string FormatAddress(string address)
    {
        return base.FormatAddress(address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(EnsureSufficientBalanceAsync)}")]
    public override Task EnsureSufficientBalanceAsync(string networkName, string address, string asset, decimal amount)
    {
        return base.EnsureSufficientBalanceAsync(networkName, address, asset, amount);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMEthereumLegacy)}{nameof(BuildTransactionAsync)}")]
    public override Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        return base.BuildTransactionAsync(networkName, transactionType, args);
    }

    #endregion
}
