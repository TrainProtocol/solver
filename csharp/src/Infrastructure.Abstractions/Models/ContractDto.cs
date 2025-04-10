using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class ContractDto
{
    public ContarctType Type { get; set; }

    public string Address { get; set; } = null!;
}
