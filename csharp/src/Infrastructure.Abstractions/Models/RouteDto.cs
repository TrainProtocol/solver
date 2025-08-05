namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDto
{
    public required TokenNetworkDto Source { get; set; } = null!;

    public required TokenNetworkDto Destination { get; set; } = null!;
}
