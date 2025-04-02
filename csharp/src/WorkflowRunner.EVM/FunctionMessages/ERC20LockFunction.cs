using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.Blockchain.EVM.FunctionMessages;

[Function("lock")]
public class ERC20LockMessage
{
    [Parameter("bytes32", "Id", 1)]
    public byte[] Id { get; set; }

    [Parameter("bytes32", "hashlock", 2)]
    public byte[] Hashlock { get; set; }

    [Parameter("uint256", "reward", 3)]
    public BigInteger Reward { get; set; }

    [Parameter("uint48", "rewardTimelock", 4)]
    public long RewardTimelock { get; set; }

    [Parameter("uint48", "timelock", 5)]
    public long Timelock { get; set; }

    [Parameter("address", "srcReciever", 6)]
    public string SourceReceiver { get; set; }

    [Parameter("string", "srcAsset", 7)]
    public string SourceAsset { get; set; }

    [Parameter("string", "dstChain", 8)]
    public string DestinationChain { get; set; }

    [Parameter("string", "dstAddress", 9)]
    public string DestinationAddress { get; set; }

    [Parameter("string", "dstAsset", 10)]
    public string DestinationAsset { get; set; }

    [Parameter("uint256", "amount", 11)]
    public BigInteger Amount { get; set; }

    [Parameter("address", "tokenContract", 12)]
    public string TokenContract { get; set; } = null!;
}

[Function("lock")]
public class ERC20LockFunction : FunctionMessage
{
    [Parameter("tuple", "params", 1)]
    public ERC20LockMessage LockParams { get; set; }
}
