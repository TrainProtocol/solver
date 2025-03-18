using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.Util;
using Nethereum.Web3;
using RedLockNet;
using Serilog;
using StackExchange.Redis;
using System.Numerics;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.EVM;

public class EthereumLegacyBlockchainService(
    SolverDbContext dbContext,
    IResilientNodeService resNodeService,
    IDistributedLockFactory distributedLockFactory, 
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) :
    BaseEVMBlockchainService(dbContext, resNodeService, distributedLockFactory, cache, privateKeyProvider)
{
    public static NetworkGroup NetworkGroup => NetworkGroup.EVM_LEGACY;
    public override async Task<Result< Fee>> EstimateFeeAsync(
            string networkName,
            string asset,
            string fromAddress,
            string toAddress,
            decimal amount,
    string? data = null)
    {
        var resultMap = new Dictionary<string, Fee>();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(new NotFoundError($"Chain setup for {networkName} is missing"));
        }

        var nodes = network.Nodes;

        if (nodes.Count == 0)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Node is not configured on {networkName} network"));
        }

        var feeCurrency = network.Tokens.SingleOrDefault(x => x.TokenContract == null);
        if (feeCurrency is null)
        {
            return Result.Fail(new BadRequestError($"No native currency"));
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == asset);
        if (currency is null)
        {
            return Result.Fail(new BadRequestError($"Invalid currency"));
        }

        if (string.IsNullOrEmpty(fromAddress))
        {
            var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.LP);

            if (managedAccount is null)
            {
                return Result.Fail(
                    new NotFoundError(
                        $"Managed address is not configured on {networkName} network"));
            }

            fromAddress = managedAccount.Address;
        }


        var gasLimitResult = await resNodeService
            .GetGasLimitAsync(nodes,
                fromAddress,
                toAddress ?? fromAddress,
                currency,
                amount,
                data);

        if (gasLimitResult.IsFailed)
        {
            Log.Error(gasLimitResult.Errors.First().Message);
            return gasLimitResult.ToResult();
        }

        var currentGasPriceResult = await resNodeService.GetGasPriceAsync(nodes);

        if (currentGasPriceResult.IsFailed)
        {
            Log.Error(currentGasPriceResult.Errors.First().Message);
            return currentGasPriceResult.ToResult();
        }

        var gasPrice = currentGasPriceResult.Value.Value.PercentageIncrease(network.FeePercentageIncrease);

        if (network.FixedGasPriceInGwei != null &&
            BigInteger.TryParse(network.FixedGasPriceInGwei, out var fixedGasPriceInGwei))
        {
            var fixedGasPriceInWei = Web3.Convert.ToWei(fixedGasPriceInGwei, UnitConversion.EthUnit.Gwei);
            gasPrice = BigInteger.Max(fixedGasPriceInWei, currentGasPriceResult.Value.Value.PercentageIncrease(20));
        }


        return Result.Ok(new Fee(
                feeCurrency.Asset,
                feeCurrency.Decimals,
                new LegacyData(gasPrice.ToString(), gasLimitResult.Value.ToString())));
    }

    public override Result<Fee> IncreaseFee(
    Fee requestFee,
    int feeIncreasePercentage)
    {
        if (requestFee.LegacyFeeData is null)
        {
            return Result.Fail("Legacy fee data is missing");
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
    public override Fee MaxFee(Fee currentFee, Fee increasedFee)
    {
        return currentFee.Amount > increasedFee.Amount ? currentFee : increasedFee;
    }
}
