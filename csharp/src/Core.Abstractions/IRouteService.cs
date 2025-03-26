using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;

namespace Train.Solver.Core.Abstractions;
public interface IRouteService
{
    Task<LimitModel?> GetLimitAsync(SourceDestinationRequest request);
    Task<QuoteModel?> GetQuoteAsync(QuoteRequest request);
    Task<IEnumerable<Token>?> GetReachablePointsAsync(bool fromSrcToDest, string? networkName, string? token);
    Task<QuoteModel?> GetValidatedQuoteAsync(QuoteRequest request);
}