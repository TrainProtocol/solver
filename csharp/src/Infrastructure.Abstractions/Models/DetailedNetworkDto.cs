using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class DetailedNetworkDto : NetworkDto
{
    public string DisplayName { get; set; } = null!;

    public string HTLCNativeContractAddress { get; set; } = null!;

    public string HTLCTokenContractAddress { get; set; } = null!;

    public TransactionFeeType FeeType { get; set; }

    public int FeePercentageIncrease { get; set; }

    public TokenDto? NativeToken { get; set; } = null!;

    public IEnumerable<TokenDto> Tokens { get; set; } = [];

    public IEnumerable<NodeDto> Nodes { get; set; } = [];
}
