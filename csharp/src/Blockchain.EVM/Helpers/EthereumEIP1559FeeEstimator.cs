using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Fee1559Suggestions;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Extensions;
using static Train.Solver.Util.Helpers.ResilientNodeHelper;

namespace Train.Solver.Blockchain.EVM.Helpers;

public class EthereumEIP1559FeeEstimator : FeeEstimatorBase
{
    public virtual double MaximumBaseFeeIncreasePerBlock => 0.125;

    public virtual int HighPriorityBlockCount => 5;

    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
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
            GetGasLimitAsync(nodes,
                request.FromAddress,
                request.ToAddress,
                currency.Contract,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(request.Network.FeePercentageIncrease);

        var fee = await GetDataFromNodesAsync(nodes,
           async url => await GetFeeAmountAsync(new Web3(url), request.Network.NativeToken!, request.Network.FeePercentageIncrease, gasLimit, HighPriorityBlockCount));

        return fee;

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
            suggestedFees.MaxPriorityFeePerGas += 1;
            suggestedFees.MaxPriorityFeePerGas =
                suggestedFees.MaxPriorityFeePerGas.Value.PercentageIncrease(feePercentageIncrease);

            return new Fee(
                feeCurrency.Symbol,
                feeCurrency.Decimals,
                new EIP1559Data(suggestedFees.MaxPriorityFeePerGas.ToString(), increasedBaseFee.ToString(),
                    gasLimit.ToString()));
        }
    }

    public override void Increase(Fee fee, int percentage)
    {
        if (fee.Eip1559FeeData == null)
        {
            throw new InvalidOperationException("Fee data is missing");
        }

        fee.Eip1559FeeData.MaxPriorityFeeInWei = BigInteger.Parse(fee.Eip1559FeeData.MaxPriorityFeeInWei)
           .PercentageIncrease(percentage)
           .ToString();
    }
}
