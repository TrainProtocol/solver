using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;
public interface IQuoteService
{
    Task<LimitDto> GetLimitAsync(SourceDestinationRequest request);

    Task<QuoteWithSolverDto> GetQuoteAsync(QuoteRequest request);

    Task<QuoteWithSolverDto> GetValidatedQuoteAsync(QuoteRequest request);
}