using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Models;

public class TransactionModel
{
    public decimal Amount { get; set; }

    public string Asset { get; set; } = null!;

    public required string NetworkName { get; set; } = null!;

    public required string TransactionHash { get; set; } = null!;
 
    public required int Confirmations { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required decimal FeeAmount { get; set; }

    public required string FeeAsset { get; set; }

    public required TransactionStatus Status { get; set; }
}
