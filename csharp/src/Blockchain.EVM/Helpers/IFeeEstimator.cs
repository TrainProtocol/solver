using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.EVM.Helpers;

public interface IFeeEstimator
{
    Task<Fee> EstimateAsync(EstimateFeeRequest request);

    void Increase(Fee fee, int percentage);

    BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction,
        EVMTransactionReceipt receipt);
}
