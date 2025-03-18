using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace Train.Solver.Core.Blockchain.EVM.FunctionMessages;

public class ERC20LockFunction : LockFunction
{
    [Parameter("uint256", "amount", 11)]
    public BigInteger Amount { get; set; }

    [Parameter("address", "tokenContract", 12)]
    public string TokenContract { get; set; } = null!;
}
