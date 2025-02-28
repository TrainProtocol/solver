using System.Numerics;
using FluentResults;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.EVM.Services;

public class EVMResilientNodeService() : IResilientNodeService
{
    private static string[] _invalidTimelockError = ["0xf8d10e82",];

    private static string[] _hashlockAlreadySetError = ["0x6e6870d5",];

    private static string[] _htlcAlreadyExistsError = ["0x3b6399ac",];

    private static string[] _alreadyClaimedError = ["0x646cf558",];

    public async Task<Result<T>> GetDataFromNodesAsync<T>(IEnumerable<Node> nodes, Func<string, Task<T>> dataRetrievalTask)
    {
        if (nodes == null)
        {
            return Result.Fail("Collection of nodes is null");
        }

        var orderedNodes = nodes
            .OrderByDescending(n => n.TraceEnabled)
            .ToList();

        if (!orderedNodes.Any())
        {
            return Result.Fail("Collection of nodes is empty");
        }
        var exceptions = new List<Exception>();
        foreach (var node in orderedNodes)
        {
            try
            {
                var taskResult = await dataRetrievalTask(node.Url);
                return taskResult;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException("All nodes failed to respond", exceptions.DistinctBy(c => new { Type = c.GetType(), c.Message }));
        }
        return Result.Fail("Failed to retrieve data from nodes");
    }

    public async Task<Result<BigInteger>> GetGasLimitAsync(
       ICollection<Node> nodes,
       string fromAddress,
       string toAddress,
       Token currency,
       decimal? amount = null,
       string? callData = null)
    {
        try
        {
            var callInput = new CallInput
            {
                From = fromAddress,
                To = toAddress,
                Value = (amount.HasValue ? Web3.Convert.ToWei(amount.Value, currency.Decimals) : BigInteger.One).ToHexBigInteger(),
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

            var estimatedGas = (await GetDataFromNodesAsync(nodes,
                async nodeUrl => await new Web3(nodeUrl).TransactionManager.EstimateGasAsync(callInput))).Value;

            return Result.Ok(estimatedGas.Value);
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
                        return Result.Fail(new InvalidTimelockError("Invalid Timelock.}"));
                    }
                    else if (e.RpcError.Data is not null && _hashlockAlreadySetError.Any(x =>
                                 e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        return Result.Fail(new HashlockAlreadySetError($"Hashlock already set."));
                    }
                    else if (e.RpcError.Data is not null && _htlcAlreadyExistsError.Any(x =>
                                 e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        return Result.Fail(new HTLCAlreadyExistsError($"HTLC already Exists."));
                    }
                    else if (e.RpcError.Data is not null && _alreadyClaimedError.Any(x =>
                                 e.RpcError.Data.ToString().Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        return Result.Fail(new AlreadyClaimedError($"HTLC already cleamed."));
                    }
                    else if (e.RpcError.Message.Contains("transfer amount exceeds balance") ||
                             e.Message.Contains("insufficient funds for gas") ||
                             e.Message.Contains("execution reverted: eth_estimateGas"))
                    {
                        return Result.Fail(
                            new InsuficientFundsForGasLimitError(
                                $"Not enough funds to estimate gas limit : {fromAddress}"));
                    }
                }
            }

            return Result.Fail($"Cannot get Gas Limit : {ae.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Cannot get Gas Limit : {ex.Message}");
        }
    }

    public async Task<Result<HexBigInteger>> GetGasPriceAsync(ICollection<Node> nodes)
    {
        try
        {
            var gasPrice = (await GetDataFromNodesAsync(nodes,
                async nodeUrl => await new Web3(nodeUrl).Eth.GasPrice.SendRequestAsync())).Value;
            return Result.Ok(gasPrice);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Cannot get Gas Price : {ex.Message}");
        }
    }

    public async Task<Result<EVMTransactionReceiptModel>> GetTransactionReceiptAsync(
        ICollection<Node> nodes,
        string transactionHash)
    {
        try
        {
            var transactionReceipt = (await GetDataFromNodesAsync(nodes,
                async nodeUrl => await new Web3(nodeUrl).Client
                    .SendRequestAsync<EVMTransactionReceiptModel>(
                        new RpcRequest(
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            "eth_getTransactionReceipt",
                            transactionHash)))).Value;

            if (transactionReceipt is null)
            {
                return Result.Fail(new TransactionReceiptNotFoundError($"Failed to get receipt. Receipt was null"));
            }

            if (!transactionReceipt.Succeeded())
            {
                return Result.Fail(new TransactionFailedError("Transaction failed"));
            }

            return Result.Ok(transactionReceipt);
        }
        catch (Exception ex)
        {
            return Result.Fail(new FluentResults.Error("Cannot get transaction receipt").CausedBy(ex));
        }
    }
}
