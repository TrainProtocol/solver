using Solnet.Wallet;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

public class HtlcSolPdaResponse
{
    public required PublicKey HtlcPublicKey { get; set; } = null!;
}
