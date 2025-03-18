using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Blockchains.EVM.FunctionMessages;
using Train.Solver.Core.Blockchains.EVM.Models;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using static Train.Solver.Core.Blockchains.EVM.Helpers.EVMResilientNodeHelper;
using static Train.Solver.Core.Helpers.ResilientNodeHelper;

namespace Train.Solver.Core.Blockchains.EVM.Activities;

public class OptimismEIP1559BlockchainActivities(
    SolverDbContext dbContext,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider)
    : EthereumEIP1559BlockchainActivities(dbContext, distributedLockFactory, cache, privateKeyProvider)
{
    protected override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
       => (receipt.L1Fee ?? BigInteger.Zero) + receipt.EffectiveGasPrice.Value * receipt.GasUsed.Value;

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(EstimateFeeAsync)}")]
    public override async Task<Fee> EstimateFeeAsync(
        string networkName, EstimateFeeRequest request)
    {
        var resultMap = new Dictionary<string, Fee>();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts).Include(network => network.DeployedContracts)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var gasPriceOracleContract = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.GasPriceOracleContract);

        if (gasPriceOracleContract == null)
        {
            throw new($"GasPriceOracleContract is not configured on {networkName} network");
        }

        var nodes = network.Nodes.OrderBy(x => x.Type != NodeType.Primary).ToList();

        if (!nodes.Any())
        {
            throw new ArgumentException("Node is not configured on {networkName} network", nameof(nodes));
        }

        var feeCurrency = network.Tokens.Single(x => x.TokenContract == null);

        var currency = network.Tokens.Single(x => x.Asset == request.Asset);

        var gasLimitResult = await GetGasLimitAsync(nodes,
                request.FromAddress,
                request.ToAddress,
                currency,
                request.Amount,
                request.CallData);

        var gasLimit = gasLimitResult;

        if (network.GasLimitPercentageIncrease != null)
        {
            gasLimit = gasLimit.PercentageIncrease(network.GasLimitPercentageIncrease.Value);
        }

        // calc miner tip
        var priorityFee = await GetDataFromNodesAsync(nodes,
            async url => await new EthMaxPriorityFeePerGas(new Web3(url).Client).SendRequestAsync());

        priorityFee = priorityFee.Value
            .PercentageIncrease(network.FeePercentageIncrease)
            .ToHexBigInteger();

        // base fee
        var pendingBlock = await GetDataFromNodesAsync(nodes,
            async url => await new Web3(url).Eth.Blocks.GetBlockWithTransactionsByNumber
                .SendRequestAsync(BlockParameter.CreatePending()));

        var baseFee = pendingBlock.BaseFeePerGas.Value
            .PercentageIncrease(1420);

        var maxFeePerGas = baseFee + priorityFee;

        var l1FeeInWei = await GetDataFromNodesAsync(nodes,
            async url => await GetL1FeeAsync(
                new Web3(url),
                currency,
                BigInteger.Parse(network.ChainId),
                priorityFee,
                maxFeePerGas,
                gasLimit,
                gasPriceOracleContract.Address,//TODO
                request.FromAddress,
                request.ToAddress,
                Web3.Convert.ToWei(request.Amount, currency.Decimals),
                request.CallData));


        return new Fee(
            feeCurrency.Asset,
            feeCurrency.Decimals,
            new EIP1559Data(
                priorityFee.Value.ToString(),
                baseFee.ToString(),
                gasLimit.ToString(),
                l1FeeInWei.PercentageIncrease(100).ToString()));

        async Task<BigInteger> GetL1FeeAsync(
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

    #region Inherited Overrides

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetBatchTransactionAsync)}")]
    public override Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds)
    {
        return base.GetBatchTransactionAsync(networkName, transactionIds);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(ComposeSignedRawTransactionAsync)}")]
    public override Task<SignedTransaction> ComposeSignedRawTransactionAsync(string networkName, string fromAddress, string toAddress, string nonce, string amountInWei, string? callData, Fee fee)
    {
        return base.ComposeSignedRawTransactionAsync(networkName, fromAddress, toAddress, nonce, amountInWei, callData, fee);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetSpenderAllowanceAsync)}")]
    public override Task<decimal> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        return base.GetSpenderAllowanceAsync(networkName, ownerAddress, spenderAddress, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(IncreaseFee)}")]
    public override Fee IncreaseFee(Fee requestFee, int feeIncreasePercentage)
    {
        return base.IncreaseFee(requestFee, feeIncreasePercentage);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetSpenderAddressAsync)}")]
    public override Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        return base.GetSpenderAddressAsync(networkName, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(PublishRawTransactionAsync)}")]
    public override Task<string> PublishRawTransactionAsync(string networkName, string fromAddress, SignedTransaction signedTransaction)
    {
        return base.PublishRawTransactionAsync(networkName, fromAddress, signedTransaction);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(BuildTransactionAsync)}")]
    public override Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        return base.BuildTransactionAsync(networkName, transactionType, args);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(EnsureSufficientBalanceAsync)}")]
    public override Task EnsureSufficientBalanceAsync(string networkName, string address, string asset, decimal amount)
    {
        return base.EnsureSufficientBalanceAsync(networkName, address, asset, amount);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(FormatAddress)}")]
    public override string FormatAddress(string address)
    {
        return base.FormatAddress(address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GenerateAddressAsync)}")]
    public override Task<string> GenerateAddressAsync(string networkName)
    {
        return base.GenerateAddressAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetBalanceAsync)}")]
    public override Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        return base.GetBalanceAsync(networkName, address, asset);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetEventsAsync)}")]
    public override Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        return base.GetEventsAsync(networkName, fromBlock, toBlock);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        return base.GetLastConfirmedBlockNumberAsync(networkName);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request)
    {
        return base.ValidateAddLockSignatureAsync(networkName, request);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(ValidateAddress)}")]
    public override bool ValidateAddress(string address)
    {
        return base.ValidateAddress(address);
    }

    [Activity(name: $"{nameof(NetworkGroup.EVMOptimismEip1559)}{nameof(GetNonceAsync)}")]
    public override Task<string> GetNonceAsync(string networkName, string address, string referenceId)
    {
        return base.GetNonceAsync(networkName, address, referenceId);
    }

    #endregion
}
