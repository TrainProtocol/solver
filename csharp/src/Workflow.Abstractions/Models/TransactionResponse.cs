using System.Numerics;
using Train.Solver.Common.Enums;

namespace Train.Solver.Workflow.Abstractions.Models;

public class TransferAction
{
    public required string From { get; set; }
    public required string To { get; set; }
    public required string Symbol { get; set; }
    public required BigInteger Amount { get; set; }
}

public class TransactionResponse
{
    public BigInteger Amount { get; set; }

    public string Asset { get; set; } = null!;

    public required int Decimals { get; set; }

    public required string NetworkName { get; set; } = null!;

    public required string TransactionHash { get; set; } = null!;

    public required int Confirmations { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required BigInteger FeeAmount { get; set; }

    public required string FeeAsset { get; set; }

    public required int FeeDecimals { get; set; }

    public required TransactionStatus Status { get; set; }

    public List<TransferAction> Actions { get; set; } = [];
}
