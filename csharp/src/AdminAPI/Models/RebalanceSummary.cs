using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.AdminAPI.Models;

public class RebalanceSummary
{
    public required ExtendedNetworkDto Network { get; set; }

    public required TokenDto Token { get; set; }

    public required string Amount { get; set; }

    public required string From  { get; set; }

    public required string To { get; set; }
}
