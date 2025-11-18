using Solnet.Wallet;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Workflow.Solana.Programs.HtlcSplProgram.Models;

public class HTLCSplRedeemRequest : HTLCSolRedeemRequest
{
    public PublicKey SourceTokenPublicKey { get; set; } = null!;

    public PublicKey RewardPublicKey { get; set; } = null!;

    public PublicKey ReceiverPublicKey { get; set; } = null!;
}
