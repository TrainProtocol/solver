using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.Workflow.EVM.FunctionMessages;

[Function("commit")]
public class CommitFunction : FunctionMessage
{
    [Parameter("string[]", "HopChains", 1)]
    public string[] HopChains { get; set; }

    [Parameter("string[]", "HopAssets", 2)]
    public string[] HopAssets { get; set; }

    [Parameter("string[]", "HopAddresses", 3)]
    public string[] HopAddresses { get; set; }

    [Parameter("string", "dstChain", 4)]
    public string DestinationChain { get; set; }

    [Parameter("string", "dstAsset", 5)]
    public string DestinationAsset { get; set; }

    [Parameter("string", "dstAddress", 6)]
    public string DestinationAddress { get; set; }

    [Parameter("string", "srcAsset", 7)]
    public string SourceAsset { get; set; }

    [Parameter("bytes32", "Id", 8)]
    public byte[] Id { get; set; }

    [Parameter("address", "srcReceiver", 9)]
    public string Receiver { get; set; }

    [Parameter("uint48", "timelock", 10)]
    public BigInteger Timelock { get; set; }
}
