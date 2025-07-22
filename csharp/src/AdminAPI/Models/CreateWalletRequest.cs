using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class CreateWalletRequest
{
    public NetworkType NetworkType { get; set; }
    public string Name { get; set; } = default!;
}