using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Train.Solver.Core.Abstractions.Repositories;
using Train.Solver.Core.DependencyInjection;

namespace Train.Solver.Repositories.Npgsql.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithNpgsqlRepositories(
        this TrainSolverBuilder builder)
    {
        return builder.WithNpgsqlRepositories(null);
    }

    public static TrainSolverBuilder WithNpgsqlRepositories(this TrainSolverBuilder builder,
       Action<EFOptions>? configureOptions)
    {
        var options = new EFOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);

        if (options.DatabaseConnectionString == null)
        {
            throw new InvalidOperationException("Azure Key Vault URI is not set.");
        }

        configureOptions?.Invoke(options);

        builder.Services.AddDbContext<SolverDbContext>(x => x.UseNpgsql(options.DatabaseConnectionString));

        builder.Services.AddTransient<INetworkRepository, EFNetworkRepository>();
        builder.Services.AddTransient<IFeeRepository, EFFeeRepository>();
        builder.Services.AddTransient<ISwapRepository, EFSwapRepository>();
        builder.Services.AddTransient<IRouteRepository, EFRouteRepository>();

        if (options.MigrateDatabase)
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SolverDbContext>();
                dbContext.Database.Migrate();
            }
        }

        return builder;
    }
}