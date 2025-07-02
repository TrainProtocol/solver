using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

public class TransactionResponse
{
    public string Amount { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public required int Decimals { get; set; }

    public required string NetworkName { get; set; } = null!;

    public required string TransactionHash { get; set; } = null!;

    public required int Confirmations { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required string FeeAmount { get; set; }

    public required string FeeAsset { get; set; }

    public required int FeeDecimals { get; set; }

    public required TransactionStatus Status { get; set; }
}
