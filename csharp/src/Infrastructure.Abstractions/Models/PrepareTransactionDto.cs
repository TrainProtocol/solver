using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class PrepareTransactionDto
{
    public string ToAddress { get; set; } = null!;

    public string? Data { get; set; }

    public string Asset { get; set; } = null!;

    public BigInteger Amount { get; set; }

    public string CallDataAsset { get; set; } = null!;

    public BigInteger CallDataAmount { get; set; } 
}
