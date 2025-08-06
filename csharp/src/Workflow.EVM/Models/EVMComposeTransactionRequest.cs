using System.Numerics;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Models;
public class EVMComposeTransactionRequest : BaseRequest
{
    public required string SignerAgentUrl { get; set; }

    public required string FromAddress { get; set; } = null!;

    public required string ToAddress { get; set; } = null!;

    public required string? CallData { get; set; }

    public required string Nonce { get; set; } = null!;

    public required BigInteger Amount { get; set; }

    public required Fee Fee { get; set; } = null!;
}
