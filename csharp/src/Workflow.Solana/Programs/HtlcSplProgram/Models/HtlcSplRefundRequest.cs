using Solnet.Wallet;
using Train.Solver.Workflow.Solana.Programs.HtlcSolProgram.Models;

namespace Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;

public class HtlcSplRefundRequest : HtlcSolRefundRequest
{
    public PublicKey SourceTokenPublicKey { get; set; } = null!;
}
