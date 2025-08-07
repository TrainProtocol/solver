using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Common.Extensions;
using Train.Solver.SmartNodeInvoker;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.Models;

namespace Train.Solver.Workflow.EVM.Helpers;

public class EthereumLegacyFeeEstimator(ISmartNodeInvoker smartNodeInvoker) : FeeEstimatorBase(smartNodeInvoker)
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
            GetGasLimitAsync(
                request.Network.Name,
                nodes,
                request.FromAddress,
                request.ToAddress,
                currency.Contract,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(request.Network.Name, nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(request.Network.FeePercentageIncrease);

        return new Fee(
            request.Network.NativeToken!.Symbol,
            request.Network.NativeToken!.Decimals,
            new LegacyData(gasPrice, gasLimitResult));
    }

    public override void Increase(Fee fee, int percentage)
    {
        if (fee.LegacyFeeData is null)
        {
            throw new ArgumentNullException(nameof(fee.LegacyFeeData), "Legacy fee data is missing");
        }

        fee.LegacyFeeData.GasPrice =
            fee.LegacyFeeData.GasPrice.PercentageIncrease(percentage);
    }
}