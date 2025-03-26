using Train.Solver.Core.Entities;

namespace Train.Solver.API.Models;

public class ContractDto
{
    public ContarctType Type { get; set; }

    public string Address { get; set; } = null!;
}
