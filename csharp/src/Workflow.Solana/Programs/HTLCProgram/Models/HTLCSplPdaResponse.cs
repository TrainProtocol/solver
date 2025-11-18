using Solnet.Wallet;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;

public class HTLCSplPdaResponse : HTLCSolPdaResponse
{
    public PublicKey HtlcTokenAccount { get; set; } = null!;

    public byte HtlcBump { get; set; }
}
