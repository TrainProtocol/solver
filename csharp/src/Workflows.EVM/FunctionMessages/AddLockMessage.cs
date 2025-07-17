using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Train.Solver.Workflows.EVM.FunctionMessages;

[Struct("addLockMsg")]
public class AddLockMessage
{
    [Parameter("bytes32", "Id", 1)]
    public byte[] Id { get; set; }

    [Parameter("bytes32", "hashlock", 2)]
    public byte[] Hashlock { get; set; }

    [Parameter("uint48", "timelock", 3)]
    public ulong Timelock { get; set; }
}
