using Train.Solver.Core.Models;

namespace Train.Solver.Blockchains.Solana.Models;

public class SolanaPublishTransactionRequest : BaseRequest
{
    public byte[] RawTx { get; set; } = null!;
}
