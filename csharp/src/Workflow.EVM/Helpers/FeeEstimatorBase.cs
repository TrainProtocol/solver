using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.SmartNodeInvoker;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.Models;

namespace Train.Solver.Workflow.EVM.Helpers;

public abstract class FeeEstimatorBase(ISmartNodeInvoker smartNodeInvoker) : IFeeEstimator
{
    public abstract Task<Fee> EstimateAsync(EstimateFeeRequest request);
    public abstract void Increase(Fee fee, int percentage);

    private static readonly string[] _invalidTimelockError = ["0xf8d10e82",];

    private static readonly string[] _hashlockAlreadySetError = ["0x6e6870d5",];

    private static readonly string[] _htlcAlreadyExistsError = ["0x3b6399ac",];

    private static readonly string[] _alreadyClaimedError = ["0x646cf558",];

    public async Task<BigInteger> GetGasLimitAsync(
        string networkName,
        IEnumerable<string> nodes,
        string fromAddress,
        string toAddress,
        string? tokenContract,
        BigInteger amount,
        string? callData = null)
    {
        var callInput = new CallInput
        {
            From = fromAddress,
            To = toAddress,
            Value = amount.ToHexBigInteger(),
        };

        if (!string.IsNullOrEmpty(callData))
        {
            callInput.Data = callData;
        }
        else if (!string.IsNullOrEmpty(tokenContract))
        {
            callInput.Value = BigInteger.Zero.ToHexBigInteger();
            callInput.To = tokenContract;

            callInput.Data = new TransferFunction
            {
                FromAddress = fromAddress,
                To = toAddress,
                Value = amount
            }.GetCallData().ToHex();
        }

        var estimatedGasResult = (await smartNodeInvoker.ExecuteAsync(networkName, nodes,
            async nodeUrl => await new Web3(nodeUrl).TransactionManager.EstimateGasAsync(callInput)));

        if (estimatedGasResult.Succeeded)
        {
            return estimatedGasResult.Data.Value;
        }
        else
        {
            foreach (var innerEx in estimatedGasResult.FailedNodes.Values)
            {
                if (innerEx is RpcResponseException e)
                {
                    if (e.RpcError.Data is not null && _invalidTimelockError.Any(x =>
                            e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        throw new InvalidTimelockException("Invalid Timelock");
                    }

                    if (e.RpcError.Data is not null && _hashlockAlreadySetError.Any(x =>
                            e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        throw new HashlockAlreadySetException("Hashlock already set");
                    }

                    if (e.RpcError.Data is not null && _htlcAlreadyExistsError.Any(x =>
                            e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        throw new HTLCAlreadyExistsException("HTLC already Exists");
                    }

                    if (e.RpcError.Data is not null && _alreadyClaimedError.Any(x =>
                            e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        throw new AlreadyClaimedExceptions("HTLC already claimed");
                    }

                    if (e.RpcError.Message.Contains("transfer amount exceeds balance") ||
                        e.Message.Contains("insufficient funds for gas") ||
                        e.Message.Contains("execution reverted: eth_estimateGas"))
                    {
                        throw new Exception("Not enough funds to estimate gas limit");
                    }
                }
            }
        }

        throw new Exception(
            $"Cannot get Gas Limit : {string.Join('\t', estimatedGasResult.FailedNodes.Values.Select(c => c.Message))}");
    }

    public async Task<HexBigInteger> GetGasPriceAsync(string networkName, IEnumerable<string> nodes)
    {
        var gasPriceResult = await smartNodeInvoker.ExecuteAsync(networkName, nodes,
            async nodeUrl => await new Web3(nodeUrl).Eth.GasPrice.SendRequestAsync());

        if (!gasPriceResult.Succeeded)
        {
            throw new AggregateException(gasPriceResult.FailedNodes.Values);
        }

        return gasPriceResult.Data;
    }

    public abstract BigInteger CalculateFee(Block block, Transaction transaction, EVMTransactionReceipt receipt);
}
