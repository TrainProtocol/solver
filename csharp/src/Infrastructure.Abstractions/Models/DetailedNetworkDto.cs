namespace Train.Solver.Infrastructure.Abstractions.Models;

public class DetailedNetworkDto : NetworkDto
{
    public string DisplayName { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public string TransactionExplorerTemplate { get; set; } = null!;

    public string AccountExplorerTemplate { get; set; } = null!;

    public string HTLCNativeContractAddress { get; set; } = null!;

    public string HTLCTokenContractAddress { get; set; } = null!;

    public DetailedTokenDto? NativeToken { get; set; } = null!;

    public IEnumerable<DetailedTokenDto> Tokens { get; set; } = [];

    public IEnumerable<NodeDto> Nodes { get; set; } = [];
}
