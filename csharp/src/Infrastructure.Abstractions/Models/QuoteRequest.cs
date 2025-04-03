namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteRequest : SourceDestinationRequest
{
    public decimal Amount { get; set; }
}
