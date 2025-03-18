using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Train.Solver.Core.Blockchains.EVM.Models;

[Event("TokenLockAdded")]
public class EtherTokenLockAddedEvent : IEventDTO
{
    [Parameter("bytes32", "Id", 1, true)]
    public virtual byte[] Id { get; set; } = null!;

    [Parameter("bytes32", "hashlock", 2, false)]
    public virtual byte[] Hashlock { get; set; } = null!;

    [Parameter("uint48", "timelock", 3, false)]
    public virtual BigInteger Timelock { get; set; }
}
