using Train.Solver.Common.Enums;

namespace Train.Solver.AdminAPI.Models;

public class CreateWalletRequest
{
    public string SignerAgent { get; set; }
    public NetworkType NetworkType { get; set; }
    public string Name { get; set; } = default!;
}