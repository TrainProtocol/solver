using Solnet.Wallet;

namespace Train.Solver.Workflow.Solana.Programs.HTLCProgram.Models;

public class HTLCSolPdaResponse
{
    public required PublicKey HtlcPublicKey { get; set; } = null!;
}
