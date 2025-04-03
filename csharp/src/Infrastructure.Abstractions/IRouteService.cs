using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;
public interface IRouteService
{
    Task<LimitModel?> GetLimitAsync(SourceDestinationRequest request);
    Task<QuoteModel?> GetQuoteAsync(QuoteRequest request);
    Task<IEnumerable<Token>?> GetReachablePointsAsync(bool fromSrcToDest, string? networkName, string? token);
    Task<QuoteModel?> GetValidatedQuoteAsync(QuoteRequest request);
}