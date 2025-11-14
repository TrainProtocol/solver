using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaPublishTransactionRequest : BaseRequest
{
    public string RawTx { get; set; } = null!;
}
