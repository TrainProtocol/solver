using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Exceptions;
using static Train.Solver.Core.Workflows.Helpers.ResilientNodeHelper;

namespace Train.Solver.Blockchains.EVM.Helpers;

public static class EVMResilientNodeHelper
{
 
    public static async Task<EVMTransactionReceipt> GetTransactionReceiptAsync(
        ICollection<Node> nodes,
        string transactionHash)
    {
        var transactionReceipt = await GetDataFromNodesAsync(nodes,
            async nodeUrl => await new Web3(nodeUrl).Client
                .SendRequestAsync<EVMTransactionReceipt>(
                    new RpcRequest(
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        "eth_getTransactionReceipt",
                        transactionHash)));

        if (transactionReceipt is null)
        {
            throw new Exception("Failed to get receipt. Receipt was null");
        }

        if (!transactionReceipt.Succeeded())
        {
            throw new TransactionFailedException("Transaction failed");
        }

        return transactionReceipt;
    }
}
