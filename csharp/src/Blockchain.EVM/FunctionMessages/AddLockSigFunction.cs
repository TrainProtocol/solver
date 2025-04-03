using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.Blockchain.EVM.FunctionMessages;

[Function("addLockSig")]
public class AddLockSigFunction : FunctionMessage
{
    [Parameter("tuple", "message", 1)]
    public AddLockMessage Message { get; set; }

    [Parameter("bytes32", "r", 2)]
    public byte[] R { get; set; }

    [Parameter("bytes32", "s", 3)]
    public byte[] S { get; set; }

    [Parameter("uint8", "v", 4)]
    public byte V { get; set; }
}
