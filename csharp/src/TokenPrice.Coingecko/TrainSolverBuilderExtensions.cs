using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Core.Abstractions;
using Train.Solver.Core.DependencyInjection;

namespace Train.Solver.TokenPrice.Coingecko;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoingeckoPrices(
        this TrainSolverBuilder builder)
    {
        builder.Services.AddTransient<ITokenPriceService, CoingeckoTokenPriceService>();
        return builder;
    }
}
