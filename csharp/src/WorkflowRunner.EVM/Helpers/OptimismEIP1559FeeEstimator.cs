using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Blockchains.EVM.FunctionMessages;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using static Train.Solver.Core.Workflows.Helpers.ResilientNodeHelper;

namespace Train.Solver.Blockchains.EVM.Helpers;

public class OptimismEIP1559FeeEstimator() : EthereumEIP1559FeeEstimator
{
    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
    {
        return (receipt.L1Fee ?? BigInteger.Zero) + receipt.EffectiveGasPrice.Value * receipt.GasUsed.Value;
    }

    public override async Task<Fee> EstimateAsync(
        Network network,
        EstimateFeeRequest request)
    {
        var gasPriceOracleContract = network.Contracts.FirstOrDefault(c => c.Type == ContarctType.GasPriceOracleContract);

        if (gasPriceOracleContract == null)
        {
            throw new($"GasPriceOracleContract is not configured on {network.Name} network");
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.NetworkName} network");
        }

        var currency = network.Tokens.Single(x => x.Asset == request.Asset);

        var gasLimit = await
            GetGasLimitAsync(nodes,
                request.FromAddress,
                request.ToAddress,
                currency,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(network.FeePercentageIncrease);

        var nativeToken = network.Tokens.Single(x=>x.TokenContract == null);

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
                nativeToken,
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
            nativeToken.Asset,
            nativeToken.Decimals,
            new EIP1559Data(
                priorityFee.Value.ToString(),
                baseFee.ToString(),
                gasLimit.ToString(),
                l1FeeInWei.PercentageIncrease(100).ToString()));
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
