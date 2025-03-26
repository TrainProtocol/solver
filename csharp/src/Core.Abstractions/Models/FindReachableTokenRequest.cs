namespace Train.Solver.Core.Abstractions.Models;

public class FindReachableTokenRequest
{
    public string Network { get; set; } = null!;

    public string Asset { get; set; } = null!;

    public bool FromSource { get; set; }
}
