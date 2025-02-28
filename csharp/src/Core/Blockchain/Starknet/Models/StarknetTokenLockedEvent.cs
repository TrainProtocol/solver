using System.Numerics;

namespace Train.Solver.Core.Blockchain.Starknet.Models;

public class StarknetTokenLockedEvent
{
    public BigInteger Hashlock { get; set; }
    
    public BigInteger Timelock { get; set; }

    public BigInteger Id { get; set; }
}
