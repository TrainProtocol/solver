using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Models;
using static Train.Solver.Core.Workflows.Helpers.ResilientNodeHelper;

namespace Train.Solver.Blockchains.EVM.Helpers;

public abstract class FeeEstimatorBase : IFeeEstimator
{
    public abstract Task<Fee> EstimateAsync(Network network, EstimateFeeRequest request);
    public abstract void Increase(Fee fee, int percentage);

    private static readonly string[] _invalidTimelockError = ["0xf8d10e82",];

    private static readonly string[] _hashlockAlreadySetError = ["0x6e6870d5",];

    private static readonly string[] _htlcAlreadyExistsError = ["0x3b6399ac",];

    private static readonly string[] _alreadyClaimedError = ["0x646cf558",];

    public static async Task<BigInteger> GetGasLimitAsync(
        ICollection<Node> nodes,
        string fromAddress,
        string toAddress,
        Token currency,
        decimal? amount = null,
        string? callData = null)
    {
        var callInput = new CallInput
        {
            From = fromAddress,
            To = toAddress,
            Value = (amount.HasValue ? Web3.Convert.ToWei(amount.Value, currency.Decimals) : BigInteger.One)
                .ToHexBigInteger(),
        };

        if (!string.IsNullOrEmpty(callData))
        {
            callInput.Data = callData;
        }
        else if (!string.IsNullOrEmpty(currency.TokenContract))
        {
            callInput.Value = BigInteger.Zero.ToHexBigInteger();
            callInput.To = currency.TokenContract;

            callInput.Data = new TransferFunction
            {
                FromAddress = fromAddress,
                To = toAddress,
                Value = Web3.Convert.ToWei(amount ?? 0, currency.Decimals)
            }.GetCallData().ToHex();
        }

        try
        {
            var estimatedGas = (await GetDataFromNodesAsync(nodes,
                async nodeUrl => await new Web3(nodeUrl).TransactionManager.EstimateGasAsync(callInput))).Value;

            return estimatedGas;
        }

        catch (AggregateException ae)
        {
            foreach (var innerEx in ae.InnerExceptions)
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

            throw new Exception(
                $"Cannot get Gas Limit : {ae.Message} {string.Join('\t', ae.InnerExceptions.Select(c => c.Message))}");
        }
    }

    public async Task<HexBigInteger> GetGasPriceAsync(ICollection<Node> nodes)
    {
        var gasPrice = await GetDataFromNodesAsync(nodes,
            async nodeUrl => await new Web3(nodeUrl).Eth.GasPrice.SendRequestAsync());
        return gasPrice;
    }

    public abstract BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceipt receipt);
}
