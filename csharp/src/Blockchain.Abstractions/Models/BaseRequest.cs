using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Blockchain.Abstractions.Models;

public class BaseRequest
{
    public required string NetworkName { get; set; } = null!;
}
