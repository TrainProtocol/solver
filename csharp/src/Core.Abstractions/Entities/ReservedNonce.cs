using Train.Solver.Core.Abstractions.Entities.Base;

namespace Train.Solver.Core.Abstractions.Entities;

public class ReservedNonce : EntityBase<int>
{
    public string Nonce { get; set; } = null!;

    public string ReferenceId { get; set; } = null!;

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
}
