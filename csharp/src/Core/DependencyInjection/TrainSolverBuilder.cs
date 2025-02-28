using Microsoft.Extensions.DependencyInjection;

namespace Train.Solver.Core.DependencyInjection;

public class TrainSolverBuilder(IServiceCollection services, TrainSolverOptions options)
{
    public IServiceCollection Services { get; } = services;

    public TrainSolverOptions Options { get; } = options;
}