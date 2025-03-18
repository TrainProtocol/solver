using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using RedLockNet;
using Serilog;
using StackExchange.Redis;
using System.Numerics;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.EVM.FunctionMessages;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.EVM;

public class OptimismEIP1559BlockchainService(
    SolverDbContext dbContext,
    IResilientNodeService resNodeService, 
    IDistributedLockFactory distributedLockFactory, 
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) 
    : EthereumEIP1559BlockchainService(dbContext, resNodeService, distributedLockFactory, cache, privateKeyProvider)
{
    public static NetworkGroup NetworkGroup => NetworkGroup.EVM_OPTIMISM_EIP1559;
    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceiptModel receipt)
         => (receipt.L1Fee ?? BigInteger.Zero) + receipt.EffectiveGasPrice.Value * receipt.GasUsed.Value;

    public override async Task<Result<Fee>> EstimateFeeAsync(
    string networkName,
    string asset,
    string fromAddress,
    string toAddress,
    decimal amount,
    string? data = null)
    {
        var resultMap = new Dictionary<string, Fee>();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts).Include(network => network.DeployedContracts)
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain setup for {networkName} is missing");
        }

        var gasPriceOracleContract = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.GasPriceOracleContract);

        if (gasPriceOracleContract is null)
        {
            return Result.Fail($"Failed to get gas price oracle contract address for network {networkName}");
        }

        var nodes = network.Nodes.OrderBy(x => x.Type != NodeType.Primary).ToList();

        if (nodes is null)
        {
            return Result.Fail(
                  new NotFoundError(
                      $"Node is not configured on {networkName} network"));
        }

        var feeCurrency = network.Tokens.SingleOrDefault(x => x.TokenContract == null);
        if (feeCurrency is null)
        {
            return Result.Fail(new BadRequestError($"No native currency"));
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == asset);
        if (currency is null)
        {
            return Result.Fail(new BadRequestError($"Invalid currency"));
        }

        if (string.IsNullOrEmpty(fromAddress))
        {
            var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.LP);

            if (managedAccount is null)
            {
                return Result.Fail(
                      new NotFoundError(
                          $"Managed address is not configured on {networkName} network"));
            }

            fromAddress = managedAccount.Address;
        }

        var gasLimitResult = await resNodeService
            .GetGasLimitAsync(nodes,
                fromAddress,
                toAddress ?? fromAddress,
                currency,
                amount,
                data);

        if (gasLimitResult.IsFailed)
        {
            Log.Error(gasLimitResult.Errors.First().Message);
            return gasLimitResult.ToResult();
        }

        var gasLimit = gasLimitResult.Value;

        if (network.GasLimitPercentageIncrease != null)
        {
            gasLimit = gasLimit.PercentageIncrease(network.GasLimitPercentageIncrease.Value);
        }

        try
        {
            // calc miner tip
            var priorityFee = (await resNodeService.GetDataFromNodesAsync(nodes,
                async url => await new EthMaxPriorityFeePerGas(new Web3(url).Client).SendRequestAsync())).Value;

            priorityFee = priorityFee.Value
                .PercentageIncrease(network.FeePercentageIncrease)
                .ToHexBigInteger();

            // base fee
            var pendingBlock = (await resNodeService.GetDataFromNodesAsync(nodes,
                async url => await new Web3(url).Eth.Blocks.GetBlockWithTransactionsByNumber
                    .SendRequestAsync(BlockParameter.CreatePending()))).Value;

            var baseFee = pendingBlock.BaseFeePerGas.Value
                .PercentageIncrease(1420);

            var maxFeePerGas = baseFee + priorityFee;

            var l1FeeInWei = (await resNodeService.GetDataFromNodesAsync(nodes,
                async url => await GetL1FeeAsync(
                    new Web3(url),
                    currency,
                    BigInteger.Parse(network.ChainId),
                    priorityFee,
                    maxFeePerGas,
                    gasLimit,
                    gasPriceOracleContract.Address,//TODO
                    fromAddress,
                    toAddress,
                    Web3.Convert.ToWei(amount, currency.Decimals),
                    data))).Value;


            return Result.Ok(new Fee(
                    feeCurrency.Asset,
                    feeCurrency.Decimals,
                    new EIP1559Data(
                        priorityFee.Value.ToString(),
                        baseFee.ToString(),
                        gasLimit.ToString(),
                        l1FeeInWei.PercentageIncrease(100).ToString())));
        }
        catch (Exception e)
        {
            return Result.Fail($"An error occurred during get fees. Error: {e.Message}");
        }
    }

    private static async Task<BigInteger> GetL1FeeAsync(
        Web3 web3,
        Token currency,
        BigInteger chainId,
        BigInteger maxPriorityFeePerGas,
        BigInteger maxFeePerGas,
        BigInteger gasLimit,
        string gasPriceOracle,
        string fromAddress,
        string toAddress,
        BigInteger value,
        string? callData)
    {
        var data = callData ?? string.Empty;

        if (!string.IsNullOrEmpty(currency.TokenContract))
        {
            var transactionMessage = new TransferFunction
            {
                FromAddress = fromAddress,
                To = toAddress,
                Value = value,
            };

            data = transactionMessage.GetCallData().ToHex();
        }

        var transaction = new Nethereum.Model.Transaction1559(
           chainId,
           1000000000,
           maxPriorityFeePerGas,
           maxFeePerGas,
           gasLimit,
           toAddress,
           value,
           data,
           accessList: []);

        var l1FeeHandler = web3.Eth.GetContractQueryHandler<L1FeeFunction>();
        var l1FeeFunctionMessage = new L1FeeFunction
        {
            Data = transaction.GetRLPEncodedRaw(),
        };

        return await l1FeeHandler.QueryAsync<BigInteger>(gasPriceOracle, l1FeeFunctionMessage);
    }
}
