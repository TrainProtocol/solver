using System.Numerics;
using Train.Solver.Common.Enums;

namespace Train.Solver.Workflow.Abstractions.Models;

public class TransactionResponse
{
    public required string NetworkName { get; set; } = null!;

    public required string TransactionHash { get; set; } = null!;

    public required int Confirmations { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required BigInteger FeeAmount { get; set; }

    public required string FeeAsset { get; set; }

    public required int FeeDecimals { get; set; }

    public required TransactionStatus Status { get; set; }
}
