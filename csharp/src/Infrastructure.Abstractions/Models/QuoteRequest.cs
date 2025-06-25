namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteRequest : SourceDestinationRequest
{
    public string Amount { get; set; } = null!;
}
