using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System.Numerics;

namespace Train.Solver.Common.Extensions;

public static class Web3ClientExtensions
{
    public static async Task<BigInteger> GetMaxPriorityFeePerGasAsync(this IWeb3 web3)
    {
        var request = new Nethereum.JsonRpc.Client.RpcRequest(Guid.NewGuid().ToString(), "eth_maxPriorityFeePerGas");

        var response = await web3.Client.SendRequestAsync<string>(request);

        return new HexBigInteger(response);
    }
}