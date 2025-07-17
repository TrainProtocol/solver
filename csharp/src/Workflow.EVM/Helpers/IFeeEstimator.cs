using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.Models;

namespace Train.Solver.Workflow.EVM.Helpers;

public interface IFeeEstimator
{
    Task<Fee> EstimateAsync(EstimateFeeRequest request);

    void Increase(Fee fee, int percentage);

    BigInteger CalculateFee(Block block, Transaction transaction,
        EVMTransactionReceipt receipt);
}
