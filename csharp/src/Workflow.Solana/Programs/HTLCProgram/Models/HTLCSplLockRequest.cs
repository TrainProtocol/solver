using Solnet.Wallet;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;

public class HTLCSplLockRequest : HTLCSolLockRequest
{
    public PublicKey SourceTokenPublicKey { get; set; } = null!;
}
