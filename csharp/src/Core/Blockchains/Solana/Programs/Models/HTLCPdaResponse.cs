using Solnet.Wallet;

namespace Train.Solver.Core.Blockchains.Solana.Programs.Models;

public class HTLCPdaResponse
{
    public PublicKey HtlcPublicKey { get; set; } = null!;

    public PublicKey HtlcTokenAccount { get; set; } = null!;

    public byte HtlcBump { get; set; }
}
