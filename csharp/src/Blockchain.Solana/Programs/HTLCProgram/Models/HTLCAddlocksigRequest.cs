using Solnet.Wallet;
using System.Numerics;

namespace Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;

public class HTLCAddlocksigRequest
{
    public byte[] Id { get; set; } = null!;

    public byte[] Hashlock { get; set; } = null!;

    public BigInteger Timelock { get; set; }

    public byte[] Signature { get; set; } = null!;

    public byte[] Message { get; set; } = null!;

    public PublicKey SenderPublicKey { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;
}
