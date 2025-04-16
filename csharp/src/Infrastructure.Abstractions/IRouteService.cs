using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;
public interface IRouteService
{
    Task<LimitDto?> GetLimitAsync(SourceDestinationRequest request);
    Task<QuoteDto?> GetQuoteAsync(QuoteRequest request);
    Task<QuoteDto?> GetValidatedQuoteAsync(QuoteRequest request);
    Task<IEnumerable<DetailedNetworkDto>?> GetSourcesAsync(string? networkName, string? token);
    Task<IEnumerable<DetailedNetworkDto>?> GetDestinationsAsync(string? networkName, string? token);
}