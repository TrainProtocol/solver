using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.Models;

namespace Train.Solver.Workflow.EVM.Helpers;

public class ArbitrumEIP1559FeeEstimator : EthereumEIP1559FeeEstimator
{
    public override BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceipt receipt)
    {
        return receipt.GasUsed.Value * receipt.EffectiveGasPrice;
    }

    public override async Task<Fee> EstimateAsync(EstimateFeeRequest request)
    {
        var fee = await base.EstimateAsync(request);

        if (fee.Eip1559FeeData is null)
        {
            throw new Exception("EIP-1559 fee data is null");
        }

        fee.Eip1559FeeData.MaxPriorityFee = BigInteger.One;

        return fee;
    }
}