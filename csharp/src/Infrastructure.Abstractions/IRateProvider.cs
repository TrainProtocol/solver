using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;

public interface IRateProvider
{
    Task<decimal> GetRateAsync(RouteDto route);

    string ProviderName { get; }
}
