using System.Numerics;
using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class CreateRouteRequest
{
    public string SourceNetworkName { get; set; } = default!;
    public string SourceToken { get; set; } = default!;
    public string SourceWalletAddress { get; set; } = default!;
    public NetworkType SourceWalletType { get; set; }

    public string DestinationNetworkName { get; set; } = default!;
    public string DestinationToken { get; set; } = default!;
    public string DestinationWalletAddress { get; set; } = default!;
    public NetworkType DestinationWalletType { get; set; }

    public string RateProvider { get; set; } = default!;
    public BigInteger MinAmount { get; set; }
    public BigInteger MaxAmount { get; set; }

    public string? ServiceFee { get; set; }
}