using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.Core.Blockchain.EVM.FunctionMessages;

[Function("commit")]
public class ERC20CreatePFunction : FunctionMessage
{
    [Parameter("string[]", "chains", 1)]
    public string[] Chains { get; set; }

    [Parameter("string[]", "dstAddresses", 2)]
    public string[] DestinationAddresses { get; set; }

    [Parameter("string", "dstChain", 3)]
    public string DestinationChain { get; set; }

    [Parameter("string", "dstAsset", 4)]
    public string DestinationAsset { get; set; }

    [Parameter("string", "dstAddress", 5)]
    public string DestinationAddress { get; set; }

    [Parameter("string", "srcAsset", 6)]
    public string SourceAsset { get; set; }

    [Parameter("address", "receiver", 7)]
    public string Receiver { get; set; }

    [Parameter("uint48", "timelock", 8)]
    public long Timelock { get; set; }

    [Parameter("uint256", "amount", 9)]
    public BigInteger Amount { get; set; }

    [Parameter("string", "tokenContract", 10)]
    public string TokenContract { get; set; }
}
