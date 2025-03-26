using System.Numerics;

namespace Train.Solver.Blockchains.Starknet.Models;

public class StarknetTokenLockedEvent
{
    public BigInteger Hashlock { get; set; }
    
    public BigInteger Timelock { get; set; }

    public BigInteger Id { get; set; }
}
