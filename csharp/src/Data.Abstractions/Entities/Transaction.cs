using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Entities;

public class Transaction : EntityBase
{
    public Network Network { get; set; } = null!;

    public int NetworkId { get; set; }

    public string TransactionHash { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }

    public string Amount { get; set; } = null!;

    public string FeeAmount { get; set; } = null!;

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; }

    public int? SwapId { get; set; }

    public virtual Swap? Swap { get; set; }
}
