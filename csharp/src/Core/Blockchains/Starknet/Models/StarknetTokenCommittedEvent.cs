using System.Numerics;

namespace Train.Solver.Core.Blockchains.Starknet.Models;

public class StarknetTokenCommittedEvent
{
    public BigInteger Id { get; set; }

    public List<string>? HopChains { get; set; }

    public List<string>? HopAssets { get; set; }

    public List<string>? HopAddress { get; set; }

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public string DestinationAsset { get; set; } = null!;

    public string SourceAsset { get; set; } = null!;

    public string AmountInBaseUnits { get; set; } = null!;

    public BigInteger Timelock { get; set; }

    public string TokenContract { get; set; } = null!;

    public string SourceReciever { get; set; } = null!;

    public string SenderAddress { get; set; } = null!;
}
