namespace Train.Solver.Infrastructure.Abstractions.Models;

public class FindReachableTokenRequest
{
    public string Asset { get; set; } = null!;

    public string NetworkName { get; set; } = null!;

    public bool FromSource { get; set; }
}
