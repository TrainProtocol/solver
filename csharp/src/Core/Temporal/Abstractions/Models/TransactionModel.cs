using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Temporal.Abstractions.Models;

public class TransactionModel
{
    public string NetworkName { get; set; } = null!;

    public string TransactionHash { get; set; } = null!;
    
    public decimal Amount { get; set; }
    
    public string Asset { get; set; } = null!;

    public int Confirmations { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public decimal? FeeAmount { get; set; }

    public string? FeeAsset { get; set; }

    public TransactionStatus Status { get; set; }
}
