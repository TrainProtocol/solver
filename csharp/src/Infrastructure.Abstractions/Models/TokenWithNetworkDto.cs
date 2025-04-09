namespace Train.Solver.Infrastructure.Abstractions.Models;

public class TokenWithNetworkDto : TokenDto
{
    public NetworkDto Network { get; set; } = null!;
}
