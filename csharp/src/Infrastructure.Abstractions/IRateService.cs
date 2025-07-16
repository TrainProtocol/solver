using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Abstractions;

public interface IRateService
{
    Task<decimal> GetRateAsync(RouteDetailedDto route);
}
