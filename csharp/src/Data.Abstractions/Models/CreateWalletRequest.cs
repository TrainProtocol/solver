using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Models;

public class CreateWalletRequest
{
    public string SignerAgent { get; set; }
    public NetworkType NetworkType { get; set; }
    public string Name { get; set; } = default!;
}