using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Infrastructure.Rate.SameAsset;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithSameAssetRateProvider(
        this TrainSolverBuilder builder)
    {
        builder.Services.AddKeyedSingleton<IRateProvider, SameAssetRateService>(Constants.ProviderName);
        return builder;
    }
}
