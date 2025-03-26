using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace Train.Solver.WorkflowRunner.EVM.FunctionMessages;

[Function("refund")]
public class RefundFunction : FunctionMessage
{
    [Parameter("bytes32", "Id ", 1)]
    public byte[] Id { get; set; }
}
