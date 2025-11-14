using Train.Solver.Common.Enums;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ExtendedNetworkDto : NetworkDto
{
    public string DisplayName { get; set; } = null!;

    public string HTLCNativeContractAddress { get; set; } = null!;

    public string HTLCTokenContractAddress { get; set; } = null!;

    public TransactionFeeType FeeType { get; set; }

    public int FeePercentageIncrease { get; set; }

    public TokenDto? NativeToken { get; set; } = null!;
}
