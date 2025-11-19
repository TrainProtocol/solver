using Solnet.Wallet;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;

public class HTLCSplLockRequest : HTLCSolLockRequest
{
    public PublicKey SourceTokenPublicKey { get; set; } = null!;
}
