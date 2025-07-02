using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Util.Shared.Options;

namespace Train.Solver.Infrastructure.MarketMaker;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithMarketMaker(
        this TrainSolverBuilder builder)
    {
        builder.Services.Configure<ExpenseFeeOptions>(
            builder.Configuration.GetSection(ExpenseFeeOptions.SectionName));

        builder.Services.AddTransient<IRouteService, RouteService>();
        builder.Services.AddSingleton<IRateService, RateService>();
        return builder;
    }
}
