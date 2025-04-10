using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Util.Extensions;

namespace Train.Solver.Blockchain.EVM.Helpers;

public class EthereumLegacyFeeEstimator : FeeEstimatorBase
{
    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
    {
        return receipt.GasUsed * transaction.GasPrice.Value;
    }

    public override async Task<EVMFeeModel> EstimateAsync(Network network, EstimateFeeRequest request)
    {
        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.NetworkName} network");
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

        return new EVMFeeModel{
            Asset = feeCurrency.Asset,
            Decimals = feeCurrency.Decimals,
            LegacyFeeData = new LegacyData(gasPrice.ToString(), gasLimitResult.ToString()) };
    }

    public override void Increase(EVMFeeModel fee, int percentage)
    {
        if (fee.LegacyFeeData is null)
        {
            throw new ArgumentNullException(nameof(fee.LegacyFeeData), "Legacy fee data is missing");
        }

        fee.LegacyFeeData.GasPriceInWei = 
            BigInteger.Parse(fee.LegacyFeeData.GasPriceInWei)
                .PercentageIncrease(percentage)
                .ToString();
    }
}
