using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Infrastructure.TokenPrice.Coingecko;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoingeckoPrices(
        this TrainSolverBuilder builder)
    {
        builder.Services.AddTransient<ITokenPriceService, CoingeckoTokenPriceService>();
        return builder;
    }
}
