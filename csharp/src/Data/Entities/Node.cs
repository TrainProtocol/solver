using Train.Solver.Data.Entities.Base;

namespace Train.Solver.Data.Entities;

public enum NodeType
{
    Primary,
    DepositTracking,
    Public,
    Secondary
}

public class Node : EntityBase<int>
{
    public string Url { get; set; } = null!;

    public NodeType Type { get; set; }

    public int NetworkId { get; set; }

    public virtual Network Network { get; set; } = null!;
    
    public bool TraceEnabled { get; set; }
    
    public double Priority { get; set; }
}
