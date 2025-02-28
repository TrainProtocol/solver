using FluentResults;
using Nethereum.RPC.Eth.DTOs;
using RedLockNet;
using StackExchange.Redis;
using System.Numerics;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Transaction = Nethereum.RPC.Eth.DTOs.Transaction;

namespace Train.Solver.Core.Blockchain.EVM;

public class ArbitrumEIP1559BlockchainService(
    SolverDbContext dbContext,
    IResilientNodeService resNodeService,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) 
    : EthereumEIP1559BlockchainService(dbContext, resNodeService, distributedLockFactory, cache, privateKeyProvider)
{
    public static NetworkGroup NetworkGroup => NetworkGroup.EVM_ARBITRUM_EIP1559;
    public override BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceiptModel receipt)
        => receipt.GasUsed.Value * receipt.EffectiveGasPrice;

    public async override Task<Result<Fee>> EstimateFeeAsync(
        string networkName,
        string asset,
        string fromAddress,
        string toAddress,
        decimal amount,
        string? data = null)
    {
        var feeResult = await base.EstimateFeeAsync(
           networkName,
           asset,
           fromAddress,
           toAddress,
           amount,
           data);

        if (feeResult.IsFailed)
        {
            return feeResult.ToResult();
        }

        feeResult.Value.Eip1559FeeData!.MaxPriorityFeeInWei = "1";

        return feeResult;
    }
}
