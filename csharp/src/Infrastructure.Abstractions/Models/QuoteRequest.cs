using System.Numerics;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class QuoteRequest : SourceDestinationRequest
{
    public BigInteger Amount { get; set; }
}
