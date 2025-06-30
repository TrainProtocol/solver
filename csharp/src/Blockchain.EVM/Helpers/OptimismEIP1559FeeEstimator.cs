using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.EVM.FunctionMessages;
using Train.Solver.Blockchain.EVM.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Util.Extensions;
using static Train.Solver.Blockchain.Common.Helpers.ResilientNodeHelper;

namespace Train.Solver.Blockchain.EVM.Helpers;

public class OptimismEIP1559FeeEstimator() : EthereumEIP1559FeeEstimator
{
    private const string GasPriceOracleContract = "0x420000000000000000000000000000000000000F";

    public override BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt)
    {
        return (receipt.L1Fee ?? BigInteger.Zero) + receipt.EffectiveGasPrice.Value * receipt.GasUsed.Value;
    }

    public override async Task<Fee> EstimateAsync(
        EstimateFeeRequest request)
    {
        var nodes = request.Network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.Network.Name} network");
        }

        var currency = request.Network.Tokens.Single(x => x.Symbol == request.Asset);

        var gasLimit = await
            GetGasLimitAsync(nodes,
                request.FromAddress,
                request.ToAddress,
                currency.Contract,
                request.Amount,
                request.CallData);

        var currentGasPriceResult = await GetGasPriceAsync(nodes);

        var gasPrice = currentGasPriceResult.Value.PercentageIncrease(request.Network.FeePercentageIncrease);

        var priorityFee = await GetDataFromNodesAsync(nodes,
                    async url => await new EthMaxPriorityFeePerGas(new Web3(url).Client).SendRequestAsync());

        priorityFee = priorityFee.Value
            .PercentageIncrease(request.Network.FeePercentageIncrease)
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
                request.Network.NativeToken,
                BigInteger.Parse(request.Network.ChainId),
                priorityFee,
                maxFeePerGas,
                gasLimit,
                GasPriceOracleContract,
                request.FromAddress,
                request.ToAddress,
                BigInteger.Parse(request.Amount),
                request.CallData));


        return new Fee(
            request.Network.NativeToken!.Symbol,
            request.Network.NativeToken!.Decimals,
            new EIP1559Data(
                priorityFee.Value.ToString(),
                baseFee.ToString(),
                gasLimit.ToString(),
                l1FeeInWei.PercentageIncrease(100).ToString()));
    }

    private static async Task<BigInteger> GetL1FeeAsync(
            Web3 web3,
            TokenDto currency,
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

        if (!string.IsNullOrEmpty(currency.Contract))
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
