using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.WorkflowRunner.Solana.Models;

public class SolanaPublishTransactionRequest : BaseRequest
{
    public byte[] RawTx { get; set; } = null!;
}
