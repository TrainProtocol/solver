using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Fee1559Suggestions;
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

public class EthereumEIP1559BlockchainService(
    SolverDbContext dbContext,
    IResilientNodeService resNodeService, 
    IDistributedLockFactory distributedLockFactory, 
    IDatabase cache, 
    IPrivateKeyProvider privateKeyProvider) 
    : BaseEVMBlockchainService(dbContext, resNodeService, distributedLockFactory, cache, privateKeyProvider)
{
    public static NetworkGroup NetworkGroup => NetworkGroup.EVM_EIP1559;
    public virtual double MaximumBaseFeeIncreasePerBlock => 0.125;

    public virtual int HighPriorityBlockCount => 5;

    public override async Task<Result<Fee>> EstimateFeeAsync(
        string networkName,
        string asset,
        string fromAddress,
        string toAddress,
        decimal amount,
        string? data = null)
    {

        var network = await dbContext.Networks
           .Include(x => x.Nodes)
           .Include(x => x.Tokens)
           .Include(x => x.ManagedAccounts)
           .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain setup for {networkName} is missing");
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            return Result.Fail(
                  new NotFoundError(
                      $"Node is not configured on {networkName} network"));
        }

        var feeCurrency = network.Tokens.SingleOrDefault(x => x.TokenContract == null);
        if (feeCurrency is null)
        {
            return Result.Fail(new BadRequestError("No native currency"));
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == asset);
        if (currency is null)
        {
            return Result.Fail(new BadRequestError("Invalid currency"));
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

        try
        {
            var gasLimitResult = await resNodeService.GetGasLimitAsync(
                    nodes,
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

            var gasLimit = gasLimitResult.Value;

            if (network.GasLimitPercentageIncrease != null)
            {
                gasLimit = gasLimit.PercentageIncrease(network.GasLimitPercentageIncrease.Value);
            }


            var fee = (await resNodeService.GetDataFromNodesAsync(nodes,
                    async url => await GetFeeAmountAsync(new Web3(url), feeCurrency, gasLimit, HighPriorityBlockCount)))
                    .Value;

            return Result.Ok(fee);

        }
        catch (Exception e)
        {
            var errorMessage = "Failed to get gas prices in EIP1559FeeProvider";
            Log.Error(e, errorMessage);
            return Result.Fail(errorMessage);
        }
    }

    protected virtual IFee1559SuggestionStrategy GetPriorityFeeHistorySuggestionStrategy(IClient client) =>
        new MedianPriorityFeeHistorySuggestionStrategy(client);

    private async Task<Fee> GetFeeAmountAsync(
        IWeb3 web3,
        Token feeCurrency,
        BigInteger gasLimit,
        int blockCount)
    {
        var suggestedFees = await GetPriorityFeeHistorySuggestionStrategy(web3.Client).SuggestFeeAsync();
        var increasedBaseFee = suggestedFees.BaseFee.Value.CompoundInterestRate(MaximumBaseFeeIncreasePerBlock, blockCount);

        // Node returns 0 but transfer service throws exception in case of 0
        suggestedFees.MaxPriorityFeePerGas += 1;
        suggestedFees.MaxPriorityFeePerGas = suggestedFees.MaxPriorityFeePerGas.Value.PercentageIncrease(feeCurrency.Network.FeePercentageIncrease);

        return new Fee(
            feeCurrency.Asset,
            feeCurrency.Decimals,
            new EIP1559Data(suggestedFees.MaxPriorityFeePerGas.ToString(), increasedBaseFee.ToString(), gasLimit.ToString()));
    }

    public override Result<Fee> IncreaseFee(Fee requestFee, int feeIncreasePercentage)
    {
        if (requestFee.Eip1559FeeData is null)
        {
            return Result.Fail("EIP1559 fee data is missing");
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

        return Result.Ok(requestFee);
    }
    public override Fee MaxFee(Fee currentFee, Fee increasedFee)
    {
        increasedFee.Eip1559FeeData!.BaseFeeInWei =
                BigInteger.Parse(currentFee.Eip1559FeeData!.BaseFeeInWei) > BigInteger.Parse(increasedFee.Eip1559FeeData!.BaseFeeInWei)
                ? currentFee.Eip1559FeeData.BaseFeeInWei
                : increasedFee.Eip1559FeeData.BaseFeeInWei;

        increasedFee.Eip1559FeeData.MaxPriorityFeeInWei =
            BigInteger.Parse(currentFee.Eip1559FeeData.MaxPriorityFeeInWei) > BigInteger.Parse(increasedFee.Eip1559FeeData.MaxPriorityFeeInWei)
            ? currentFee.Eip1559FeeData.MaxPriorityFeeInWei
            : increasedFee.Eip1559FeeData.MaxPriorityFeeInWei;

        return increasedFee;
    }
}
