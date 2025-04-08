namespace Train.Solver.API.Models;

public class SwapDto
{
    public string CommitId { get; set; } = null!;

    public string SourceNetwork { get; set; } = null!;

    public string SourceToken { get; set; } = null!;

    public decimal SourceAmount { get; set; }

    public string SourceAddress { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationToken { get; set; } = null!;

    public decimal DestinationAmount { get; set; }

    public string DestinationAddress { get; set; } = null!;

    public decimal FeeAmount { get; set; }

    public List<TransactionDto> Transactions { get; set; } = [];
}
