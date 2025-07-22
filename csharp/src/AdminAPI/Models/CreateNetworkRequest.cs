using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class CreateNetworkRequest
{
    public string NetworkName { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public NetworkType Type { get; set; }
    public TransactionFeeType FeeType { get; set; }
    public string ChainId { get; set; } = default!;
    public int FeePercentageIncrease { get; set; }
    public string HtlcNativeContractAddress { get; set; } = default!;
    public string HtlcTokenContractAddress { get; set; } = default!;
    public string NativeTokenSymbol { get; set; } = default!;
    public string NativeTokenContract { get; set; } = default!;
    public int NativeTokenDecimals { get; set; }
}
