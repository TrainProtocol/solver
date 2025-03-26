using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.API.Models;

public class NodeDto
{
    public string Url { get; set; } = null!;

    public NodeType Type { get; set; }
}
