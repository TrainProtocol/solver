namespace Train.Solver.Infrastructure.Abstractions.Models;

public class SwapDto
{
    public string CommitId { get; set; } = null!;

    public string SourceNetwork { get; set; } = null!;

    public string SourceToken { get; set; } = null!;

    public string SourceAmount { get; set; } = null!;

    public decimal SourceAmountInUsd { get; set; }

    public string SourceAddress { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationToken { get; set; } = null!;

    public string DestinationAmount { get; set; } = null!;

    public decimal DestinationAmountInUsd { get; set; }

    public string DestinationAddress { get; set; } = null!;

    public string FeeAmount { get; set; } = null!;

    public IEnumerable<TransactionDto> Transactions { get; set; } = [];
}
