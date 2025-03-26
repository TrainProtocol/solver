using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.WorkflowRunner.EVM.Models;

namespace Train.Solver.WorkflowRunner.EVM.Helpers;

public interface IFeeEstimator
{
    Task<Fee> EstimateAsync(
        Network network,
        EstimateFeeRequest request);

    void Increase(Fee fee, int percentage);

    BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction,
        EVMTransactionReceipt receipt);
}
