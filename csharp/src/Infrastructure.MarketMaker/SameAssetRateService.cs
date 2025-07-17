using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Rate.SameAsset;

public class SameAssetRateService : IRateProvider
{
    public string ProviderName => Constants.ProviderName;

    public Task<decimal> GetRateAsync(RouteDto _)
    {
        return Task.FromResult(1m);
    }
}
