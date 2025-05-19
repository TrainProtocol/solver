using Solnet.Wallet;
using System.Numerics;

namespace Train.Solver.Blockchain.Solana.Models;

public class SolanaAddLockSigMessageRequest
{
    public byte[] Id { get; set; } = null!;

    public byte[] Hashlock { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public BigInteger Timelock { get; set; }
}
