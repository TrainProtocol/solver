using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ManagedAccountDto
{
    public string Address { get; set; } = null!;

    public NetworkType NetworkType { get; set; }
}
