using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Common.Extensions;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.Models;
using Train.Solver.SmartNodeInvoker;

namespace Train.Solver.Workflow.EVM.Helpers;

public class EthereumEIP1559FeeEstimator(ISmartNodeInvoker smartNodeInvoker) : FeeEstimatorBase(smartNodeInvoker)
{
    public virtual double MaximumBaseFeeIncreasePerBlock => 0.125;

    public virtual int HighPriorityBlockCount => 5;

    private const string MinMaxPriorityFeePerGas = "1";

    public override BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceipt receipt)
    {
        return receipt.GasUsed * (block.BaseFeePerGas + transaction.MaxPriorityFeePerGas.Value);
    }

    public override async Task<Fee> EstimateAsync(EstimateFeeRequest request)
    {
        var nodes = request.Network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.Network.Name} network");
        }

        var currency = request.Network.Tokens.Single(x => x.Symbol == request.Asset);

        var gasLimit = await
            GetGasLimitAsync(request.Network.Name, nodes,
                request.FromAddress,
                request.ToAddress,
                currency.Contract,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(request.Network.Name, nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(request.Network.FeePercentageIncrease);

        var feeResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, nodes,
           async url => await GetFeeAmountAsync(new Web3(url), request.Network.NativeToken!, request.Network.FeePercentageIncrease, gasLimit, HighPriorityBlockCount));

        if (!feeResult.Succeeded)
        {
            throw new AggregateException(feeResult.FailedNodes.Values);
        }

        return feeResult.Data;

        async Task<Fee> GetFeeAmountAsync(
            IWeb3 web3,
            TokenDto feeCurrency,
            int feePercentageIncrease,
            BigInteger gasLimit,
            int blockCount)
        {
            var suggestedFees = await new MedianPriorityFeeHistorySuggestionStrategy(web3.Client).SuggestFeeAsync();
            var increasedBaseFee =
                suggestedFees.BaseFee.Value.CompoundInterestRate(MaximumBaseFeeIncreasePerBlock, blockCount);

            // Node returns 0 but transfer service throws exception in case of 0
            var maxPriorityFeePerGas = BigInteger.Parse(MinMaxPriorityFeePerGas);
            maxPriorityFeePerGas += (await web3.GetMaxPriorityFeePerGasAsync()).PercentageIncrease(feePercentageIncrease);

            return new Fee(
                feeCurrency.Symbol,
                feeCurrency.Decimals,
                new EIP1559Data(
                    maxPriorityFeePerGas,
                    increasedBaseFee,
                    gasLimit));
        }
    }

    public override void Increase(Fee fee, int percentage)
    {
        if (fee.Eip1559FeeData == null)
        {
            throw new InvalidOperationException("Fee data is missing");
        }

        fee.Eip1559FeeData.MaxPriorityFee = fee.Eip1559FeeData.MaxPriorityFee
           .PercentageIncrease(percentage);
    }
}
