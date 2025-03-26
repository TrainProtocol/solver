using Train.Solver.Core.Abstractions.Entities;

namespace Train.Solver.Core.Abstractions.Models;

public class NetworkModel
{
    public string Name { get; set; } = null!;

    public NetworkType Type { get; set; }
}
