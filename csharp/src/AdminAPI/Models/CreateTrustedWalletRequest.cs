using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class CreateTrustedWalletRequest
{
    public NetworkType NetworkType { get; set; }
    public string Address { get; set; } = default!;
    public string Name { get; set; } = default!;
}
