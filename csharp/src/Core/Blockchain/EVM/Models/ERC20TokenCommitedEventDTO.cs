using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Train.Solver.Core.Blockchain.EVM.Models;

public class ERC20TokenCommitedEventDTO : EtherTokenCommittedEventDTO
{
    [Parameter("address", "tokenContract", 13, false)]
    public virtual string TokenContract { get; set; } = null!;
}
