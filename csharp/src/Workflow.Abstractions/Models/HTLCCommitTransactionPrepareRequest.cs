using System.Numerics;

namespace Train.Solver.Workflow.Abstractions.Models;

public class HTLCCommitTransactionPrepareRequest
{
    public string Receiever { get; set; } = null!;

    public string[] HopChains { get; set; }

    public string[] HopAssets { get; set; }

    public string[] HopAddresses { get; set; }

    public string DestinationChain { get; set; } = null!;

    public string DestinationAsset { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public string SourceAsset { get; set; } = null!;

    public long Timelock { get; set; }

    public BigInteger Amount { get; set; }

    public string Id { get; set; } = null!;
}
