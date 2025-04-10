using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class NodeDto
{
    public string Url { get; set; } = null!;

    public NodeType Type { get; set; }
}
