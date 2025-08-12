using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class DetailedNetworkDto : ExtendedNetworkDto
{
    public IEnumerable<TokenDto> Tokens { get; set; } = [];

    public IEnumerable<NodeDto> Nodes { get; set; } = [];
}
