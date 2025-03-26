using Train.Solver.Core.Entities;
using Train.Solver.Core.Models;

namespace Train.Solver.Core.Services;
public interface IRouteService
{
    Task<LimitModel?> GetLimitAsync(SourceDestinationRequest request);
    Task<QuoteModel?> GetQuoteAsync(QuoteRequest request);
    Task<IEnumerable<Token>?> GetReachablePointsAsync(bool fromSrcToDest, string? networkName, string? token);
    Task<QuoteModel?> GetValidatedQuoteAsync(QuoteRequest request);
}