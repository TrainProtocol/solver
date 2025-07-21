using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCLockTransactionPrepareRequest
{
    public string Receiver { get; set; } = null!;

    public string Hashlock { get; set; } = null!;

    public long Timelock { get; set; }

    public string SourceAsset { get; set; } = null!;

    public string SourceNetwork { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public string DestinationAsset { get; set; } = null!;

    public string CommitId { get; set; } = null!;

    public BigInteger Amount { get; set; }

    public BigInteger Reward { get; set; }

    public long RewardTimelock { get; set; }
}
