using Solnet.Wallet;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaAddLockSigMessageRequest
{
    public byte[] Id { get; set; } = null!;

    public byte[] Hashlock { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public long Timelock { get; set; }
}
