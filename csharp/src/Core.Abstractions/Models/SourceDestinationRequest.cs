namespace Train.Solver.Core.Abstractions.Models;

public class SourceDestinationRequest
{
    public string SourceNetwork { get; set; } = null!;

    public string SourceToken { get; set; } = null!;

    public string DestinationNetwork { get; set; } = null!;

    public string DestinationToken { get; set; } = null!;
}
