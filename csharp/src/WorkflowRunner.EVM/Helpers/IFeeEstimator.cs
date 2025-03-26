using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.EVM.Helpers;

public interface IFeeEstimator
{
    Task<Fee> EstimateAsync(
        Network network,
        EstimateFeeRequest request);

    void Increase(Fee fee, int percentage);

    BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction,
        EVMTransactionReceipt receipt);
}
