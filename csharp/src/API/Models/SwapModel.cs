namespace Train.Solver.API.Models;

public class SwapModel
{
    public string CommitId { get; set; } = null!;

    public string SourceNetwork { get; set; } = null!;

    public string SourceAsset { get; set; } = null!;

    public decimal SourceAmount { get; set; }

    public string SourceAddress { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationAsset { get; set; } = null!;

    public decimal DestinationAmount { get; set; }

    public string DestinationAddress { get; set; } = null!;

    public decimal FeeAmount { get; set; }

    public List<TransactionModel> Transactions { get; set; } = [];
}
