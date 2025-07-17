using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Train.Solver.Workflow.EVM.Models;

[Event("TokenCommitted")]
public class EtherTokenCommittedEvent : IEventDTO
{
    [Parameter("bytes32", "Id", 1, true)]
    public virtual byte[] Id { get; set; } = null!;

    [Parameter("string[]", "hopChains", 2, false)]
    public virtual List<string>? HopChains { get; set; }

    [Parameter("string[]", "hopAssets", 3, false)]
    public virtual List<string>? HopAssets { get; set; }

    [Parameter("string[]", "hopAddresses", 4, false)]
    public virtual List<string>? HopAddresses { get; set; }

    [Parameter("string", "dstChain", 5, false)]
    public virtual string DestinationChain { get; set; } = null!;

    [Parameter("string", "dstAddress", 6, false)]
    public virtual string DestinationAddress { get; set; } = null!;

    [Parameter("string", "dstAsset", 7, false)]
    public virtual string DestinationAsset { get; set; } = null!;

    [Parameter("address", "sender", 8, true)]
    public virtual string Sender { get; set; } = null!;

    [Parameter("address", "srcReceiver", 9, true)]
    public virtual string Receiver { get; set; } = null!;

    [Parameter("string", "srcAsset", 10, false)]
    public virtual string SourceAsset { get; set; } = null!;

    [Parameter("uint256", "amount", 11, false)]
    public virtual BigInteger Amount { get; set; }

    [Parameter("uint48", "timelock", 12, false)]
    public virtual BigInteger Timelock { get; set; }
}
