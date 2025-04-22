using Solnet.Wallet;
using Train.Solver.Blockchain.Solana.Models;

namespace Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;

public class HTLCAddlocksigRequest
{
    public SolanaAddLockSigMessageRequest AddLockSigMessageRequest { get; set; } = null!;

    public byte[] Signature { get; set; } = null!;

    public byte[] Message { get; set; } = null!;

    public PublicKey SenderPublicKey { get; set; } = null!;
}
