using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Train.Solver.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(
         this IServiceCollection services,
         string connectionString) => services.AddDbContext<SolverDbContext>(
            options => options.UseNpgsql(connectionString));
}
