using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.Workflow.EVM.FunctionMessages;

[Function("getL1Fee", "uint256")]
public class L1FeeFunction : FunctionMessage
{
    [Parameter("bytes", "_data")] public byte[] Data { get; set; }
}