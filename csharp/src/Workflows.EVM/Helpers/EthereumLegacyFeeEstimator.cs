using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Common.Extensions;
using Train.Solver.Workflows.Abstractions.Models;
using Train.Solver.Workflows.EVM.Models;

namespace Train.Solver.Workflows.EVM.Helpers;

public class EthereumLegacyFeeEstimator : FeeEstimatorBase
{
    public override BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceipt receipt)
    {
        return receipt.GasUsed * transaction.GasPrice.Value;
    }

    public override async Task<Fee> EstimateAsync(EstimateFeeRequest request)
    {
        var nodes = request.Network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.Network.Name} network");
        }

        var currency = request.Network.Tokens.Single(x => x.Symbol == request.Asset);

        var gasLimitResult = await
            GetGasLimitAsync(nodes,
                request.FromAddress,
                request.ToAddress,
                currency.Contract,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(request.Network.FeePercentageIncrease);

        return new Fee(
            request.Network.NativeToken!.Symbol,
            request.Network.NativeToken!.Decimals,
            new LegacyData(gasPrice.ToString(), gasLimitResult.ToString()));
    }

    public override void Increase(Fee fee, int percentage)
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

//protected override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
//    => receipt.GasUsed.Value * receipt.EffectiveGasPrice;

