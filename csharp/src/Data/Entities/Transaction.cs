﻿using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public enum TransactionType
{
    Transfer,
    Approve,
    HTLCCommit,
    HTLCLock,
    HTLCRedeem,
    HTLCRefund,
    HTLCAddLockSig,
}


public enum TransactionStatus
{
    Completed,
    Initiated,
    Failed,
}

public class Transaction : EntityBase<Guid>
{
    public string? TransactionId { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public string NetworkName { get; set; } = null!;

    public int Confirmations { get; set; }

    public string? Asset { get; set; }

    public decimal Amount { get; set; }

    public decimal UsdPrice { get; set; }
    
    public string? FeeAsset { get; set; }

    public decimal? FeeAmount { get; set; }

    public decimal? FeeUsdPrice { get; set; }

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; }

    public string? SwapId { get; set; }

    public virtual Swap? Swap { get; set; }
}
