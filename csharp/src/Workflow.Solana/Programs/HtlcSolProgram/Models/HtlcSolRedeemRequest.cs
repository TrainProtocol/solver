using Solnet.Wallet;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

public class HTLCSolRedeemRequest
{
    public byte[] Id { get; set; } = null!;

    public byte[] Secret { get; set; } = null!;

    public PublicKey ReceiverPublicKey { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public PublicKey SenderPublicKey { get; set; } = null!;
}
