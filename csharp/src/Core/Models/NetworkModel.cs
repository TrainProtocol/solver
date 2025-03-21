using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Models;

public class NetworkModel
{
    public string Name { get; set; } = null!;

    public NetworkGroup Group { get; set; }
}
