using Train.Solver.API.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;
public interface IRouteService
{
    Task<LimitDto?> GetLimitAsync(SourceDestinationRequest request);
    Task<QuoteDto?> GetQuoteAsync(QuoteRequest request);
    Task<QuoteDto?> GetValidatedQuoteAsync(QuoteRequest request);
    Task<IEnumerable<NetworkWithTokensDto>?> GetSourcesAsync(string? networkName, string? token);
    Task<IEnumerable<NetworkWithTokensDto>?> GetDestinationsAsync(string? networkName, string? token);
}