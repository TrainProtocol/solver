using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Entities;

public class Route : EntityBase
{
    public decimal MaxAmountInSource { get; set; }

    public int SourceTokenId { get; set; }

    public int DestinationTokenId { get; set; }

    public int SourceWalletId { get; set; }

    public int DestinationWalletId { get; set; }

    public int RateProviderId { get; set; }

    public int? ServiceFeeId { get; set; }

    public RouteStatus Status { get; set; }

    public RateProvider RateProvider { get; set; } = null!;

    public Token SourceToken { get; set; } = null!;

    public Token DestinationToken { get; set; } = null!;

    public Wallet SourceWallet { get; set; } = null!;

    public Wallet DestinationWallet { get; set; } = null!;

    public ServiceFee? ServiceFee { get; set; }
}