using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class NetworkModel
{
    public string Name { get; set; } = null!;

    public NetworkType Type { get; set; }
}
