using Solnet.Wallet;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;

public class HTLCSplPdaResponse : HtlcSolPdaResponse
{
    public PublicKey HtlcTokenAccount { get; set; } = null!;

    public byte HtlcBump { get; set; }
}
