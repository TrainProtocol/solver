using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.Workflow.EVM.FunctionMessages;

[Function("approve", "bool")]
public class ApproveFunction : FunctionMessage
{
    [Parameter("address", "spender", 1)]
    public virtual string Spender { get; set; }

    [Parameter("uint256", "value", 2)]
    public virtual BigInteger Value { get; set; }
}
