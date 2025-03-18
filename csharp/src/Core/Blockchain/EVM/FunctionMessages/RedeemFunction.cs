using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

namespace Train.Solver.Core.Blockchain.EVM.FunctionMessages;

[Function("redeem")]
public class RedeemFunction : FunctionMessage
{
    [Parameter("bytes32", "Id", 1)]
    public byte[] Id { get; set; }

    [Parameter("uint256", "secret", 2)]
    public BigInteger Secret { get; set; }
}
