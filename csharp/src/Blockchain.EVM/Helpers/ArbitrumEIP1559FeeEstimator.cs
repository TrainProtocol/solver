using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.Models;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.EVM.Helpers;

public class ArbitrumEIP1559FeeEstimator : EthereumEIP1559FeeEstimator
{
    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
    {
        return receipt.GasUsed.Value * receipt.EffectiveGasPrice;
    }

    public override async Task<EVMFeeModel> EstimateAsync(Network network, EstimateFeeRequest request)
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
