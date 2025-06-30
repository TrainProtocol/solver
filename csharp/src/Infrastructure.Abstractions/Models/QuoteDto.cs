using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteDto
{
    public BigInteger TotalFee { get; set; } 

    //public decimal TotalFeeInUsd { get; set; }

    public BigInteger ReceiveAmount { get; set; }

    //public decimal ReceiveAmountInUsd { get; set; }

    //public required BigInteger SourceAmount { get; set; }

    //public required decimal SourceAmountInUsd { get; set; }
}