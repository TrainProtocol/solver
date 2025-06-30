using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;
public interface IRouteService
{
    Task<LimitDto?> GetLimitAsync(SourceDestinationRequest request);

    Task<QuoteWithSolverDto?> GetQuoteAsync(QuoteRequest request);

    Task<QuoteWithSolverDto> GetValidatedQuoteAsync(QuoteRequest request);

    Task<IEnumerable<DetailedNetworkDto>?> GetSourcesAsync(string? networkName, string? token);

    Task<IEnumerable<DetailedNetworkDto>?> GetDestinationsAsync(string? networkName, string? token);
}