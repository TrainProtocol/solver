namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteDto
{
    public string TotalFee { get; set; } = null!;

    public decimal TotalFeeInUsd { get; set; }

    public string ReceiveAmount { get; set; } = null!;

    public decimal ReceiveAmountInUsd { get; set; }

    public required string SourceAmount { get; set; }

    public required decimal SourceAmountInUsd { get; set; }
}