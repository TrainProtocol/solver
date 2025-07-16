using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Util.Enums;

namespace Train.Solver.Data.Abstractions.Entities;


public enum TransactionStatus
{
    Completed,
    Initiated,
    Failed,
}

public class Transaction : EntityBase
{
    public string TransactionHash { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }

    public string NetworkName { get; set; } = null!;

    public int Confirmations { get; set; }

    public Token Token { get; set; } = null!;

    public int TokenId { get; set; }

    public string Amount { get; set; } = null!;

    public Token FeeToken { get; set; } = null!;

    public int FeeTokenId { get; set; }

    public string FeeAmount { get; set; } = null!;

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; }

    public int? SwapId { get; set; }

    public virtual Swap? Swap { get; set; }
}
