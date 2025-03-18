using FluentResults;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Core.Blockchain.Models;

namespace Train.Solver.Core.Blockchain.EVM;

public interface IEVMBlockchainService : IBlockchainService
{
    BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceiptModel receipt);

    Result<Fee> IncreaseFee(Fee requestFee, int feeIncreasePercentage);

    Fee MaxFee(Fee currentFee, Fee increasedFee);

    Task<Result<string>> PublishRawTransactionAsync(
        string networkName,
        string fromAddress,
        SignedTransaction signedTransaction);

    Task<Result<SignedTransaction>> ComposeSignedRawTransactionAsync(
        string networkName,
        string fromAddress,
        string toAddress,
        string nonce,
        string amountInWei,
        string? callData,
        Fee fee);
}
