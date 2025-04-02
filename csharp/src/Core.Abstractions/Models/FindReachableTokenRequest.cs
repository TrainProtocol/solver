namespace Train.Solver.Core.Abstractions.Models;

public class FindReachableTokenRequest : BaseRequest
{
    public string Asset { get; set; } = null!;

    public bool FromSource { get; set; }
}
