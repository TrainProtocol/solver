using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class UpdateNetworkRequest
{
    public string DisplayName { get; set; } = default!;
    public TransactionFeeType FeeType { get; set; }
    public int FeePercentageIncrease { get; set; }
    public string HtlcNativeContractAddress { get; set; } = default!;
    public string HtlcTokenContractAddress { get; set; } = default!;
}

