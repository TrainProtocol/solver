using Train.Solver.API.Models;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDto
{
    public int Id { get; set; }

    public TokenWithNetworkDto Source { get; set; } = null!;

    public TokenWithNetworkDto Destionation { get; set; } = null!;

    public decimal MaxAmountInSource { get; set; }

    public RouteStatus Status { get; set; }
}
