using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteDto
{
    public BigInteger TotalFee { get; set; } 

    public BigInteger ReceiveAmount { get; set; }
}