namespace Train.Solver.Core.Abstractions.Models;

public class QuoteRequest : SourceDestinationRequest
{
    public decimal Amount { get; set; }
}
