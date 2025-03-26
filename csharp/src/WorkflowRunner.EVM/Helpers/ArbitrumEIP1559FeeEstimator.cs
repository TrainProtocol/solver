using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.Blockchains.EVM.Helpers;

public class ArbitrumEIP1559FeeEstimator : EthereumEIP1559FeeEstimator
{
    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
    {
        return receipt.GasUsed.Value * receipt.EffectiveGasPrice;
    }

    public override async Task<Fee> EstimateAsync(Network network, EstimateFeeRequest request)
    {
        var fee = await base.EstimateAsync(network, request);

        if (fee.Eip1559FeeData is null)
        {
            throw new Exception("EIP-1559 fee data is null");
        }

        fee.Eip1559FeeData.MaxPriorityFeeInWei = "1";

        return fee;
    }
}

//protected override BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceipt receipt)
//        => receipt.GasUsed.Value * receipt.EffectiveGasPrice;