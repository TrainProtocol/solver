namespace Train.Solver.API.Models;

public class NetworkWithTokensDto : NetworkDto
{
    public List<TokenDto> Tokens { get; set; } = [];

    public virtual List<NodeDto> Nodes { get; set; } = new();

    public virtual List<ContractDto> Contracts { get; set; } = new();

    public virtual List<ManagedAccountDto> ManagedAccounts { get; set; } = new();
}
