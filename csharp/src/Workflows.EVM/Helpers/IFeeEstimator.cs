using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Workflows.Abstractions.Models;
using Train.Solver.Workflows.EVM.Models;

namespace Train.Solver.Workflows.EVM.Helpers;

public interface IFeeEstimator
{
    Task<Fee> EstimateAsync(EstimateFeeRequest request);

    void Increase(Fee fee, int percentage);

    BigInteger CalculateFee(Block block, Transaction transaction,
        EVMTransactionReceipt receipt);
}
