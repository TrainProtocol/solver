namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteDto
{
    public TokenNetworkDto Source { get; set; } = null!;

    public TokenNetworkDto Destination { get; set; } = null!;
}
