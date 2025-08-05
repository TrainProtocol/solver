using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Infrastructure.Rate.Binance;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithBinanceRateProvider(
        this TrainSolverBuilder builder)
    {
        builder.Services.AddKeyedSingleton<IRateProvider, BinanceRateService>(Constants.ProviderName);
        return builder;
    }
}
