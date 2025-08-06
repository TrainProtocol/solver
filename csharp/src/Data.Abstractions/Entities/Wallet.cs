using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Abstractions.Entities;

public class Wallet : EntityBase
{
    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public NetworkType NetworkType { get; set; }

    public int SignerAgentId { get; set; }

    public SignerAgent SignerAgent { get; set; } = null!;
}
