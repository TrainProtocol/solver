using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Models;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSplProgram.Models;

public class HtlcAddlocksigRequest
{
    public SolanaAddLockSigMessageRequest AddLockSigMessageRequest { get; set; } = null!;

    public byte[] Signature { get; set; } = null!;

    public PublicKey SenderPublicKey { get; set; } = null!;
}
