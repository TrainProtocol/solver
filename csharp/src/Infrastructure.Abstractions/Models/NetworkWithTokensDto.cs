namespace Train.Solver.API.Models;

public class NetworkWithTokensDto : NetworkDto
{
    public IEnumerable<TokenDto> Tokens { get; set; } = [];

    public IEnumerable<NodeDto> Nodes { get; set; } = [];

    public IEnumerable<ContractDto> Contracts { get; set; } = [];

    public IEnumerable<ManagedAccountDto> ManagedAccounts { get; set; } = [];
}
