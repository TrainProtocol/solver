namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteDto
{
    public decimal TotalFee { get; set; }

    public decimal TotalFeeInUsd { get; set; }

    public decimal ReceiveAmount { get; set; }

    public decimal ReceiveAmountInUsd { get; set; }
}
