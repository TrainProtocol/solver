using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public class ReservedNonce : EntityBase<int>
{
    public string Nonce { get; set; } = null!;

    public string ReferenceId { get; set; } = null!;

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
}
