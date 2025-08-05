using Train.Solver.Blockchain.Abstractions.Models;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaPublishTransactionRequest : BaseRequest
{
    public byte[] RawTx { get; set; } = null!;
}
