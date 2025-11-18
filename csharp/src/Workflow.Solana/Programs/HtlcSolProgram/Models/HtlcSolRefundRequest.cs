using Solnet.Wallet;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

public class HtlcSolRefundRequest
{
    public byte[] Id { get; set; } = null!;

    public PublicKey SignerPublicKey { get; set; } = null!;

    public PublicKey ReceiverPublicKey { get; set; } = null!;
}
