using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

public class TransactionResponse
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
