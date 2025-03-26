using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Train.Solver.Blockchains.EVM.Models;

public class ERC20TokenCommitedEvent : EtherTokenCommittedEvent
{
    [Parameter("address", "tokenContract", 13, false)]
    public virtual string TokenContract { get; set; } = null!;
}
