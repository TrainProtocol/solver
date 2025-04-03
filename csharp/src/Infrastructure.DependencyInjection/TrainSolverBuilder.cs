using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Train.Solver.Infrastructure.DependencyInjection;

public class TrainSolverBuilder(IServiceCollection services, IConfiguration configuration, TrainSolverOptions options)
{
    public IServiceCollection Services { get; } = services;

    public IConfiguration Configuration { get; } = configuration;

    public TrainSolverOptions Options { get; } = options;
}