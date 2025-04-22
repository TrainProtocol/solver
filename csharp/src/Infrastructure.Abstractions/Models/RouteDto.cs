using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDto
{
    public int Id { get; set; }

    public TokenNetworkDto Source { get; set; } = null!;

    public TokenNetworkDto Destionation { get; set; } = null!;

    public decimal MaxAmountInSource { get; set; }

    public RouteStatus Status { get; set; }
}
