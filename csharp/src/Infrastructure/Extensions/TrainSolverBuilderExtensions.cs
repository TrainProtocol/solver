using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Services;

namespace Train.Solver.Infrastructure.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoreServices(
        this TrainSolverBuilder builder)
    {
        builder.Services.AddTransient<IRouteService, RouteService>();
        builder.Services.AddTransient(typeof(KeyedServiceResolver<>));

        return builder;
    }
}
