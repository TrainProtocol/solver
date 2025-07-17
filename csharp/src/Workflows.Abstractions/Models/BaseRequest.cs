using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflows.Abstractions.Models;

public class BaseRequest
{
    public required DetailedNetworkDto Network { get; set; } = null!;
}
