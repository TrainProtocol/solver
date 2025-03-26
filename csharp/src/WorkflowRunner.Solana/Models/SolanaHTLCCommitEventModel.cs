namespace Train.Solver.Blockchains.Solana.Models;

public class SolanaHTLCCommitEventModel
{
    public string Id { get; set; } = null!;

    public decimal Amount { get; set; }

    public string AmountInWei { get; set; } = null!;

    public string ReceiverAddress { get; set; } = null!;

    public string SenderAddress { get; set; } = null!;

    public string SourceAsset { get; set; } = null!;

    public string DestinationAddress { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationAsset { get; set; } = null!;

    public long TimeLock { get; set; }
}
