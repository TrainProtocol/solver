using Train.Solver.Core.Entities;

namespace Train.Solver.API.Models;

public class ManagedAccountDto
{
    public string Address { get; set; } = null!;

    public AccountType Type { get; set; }
}
