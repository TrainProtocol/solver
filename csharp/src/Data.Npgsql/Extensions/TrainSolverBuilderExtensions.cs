using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Data.Npgsql.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithNpgsqlRepositories(this TrainSolverBuilder builder)
    {
        return builder.WithNpgsqlRepositories(null);
    }

    public static TrainSolverBuilder WithNpgsqlRepositories(
        this TrainSolverBuilder builder,
        Action<EFOptions>? configureOptions)
    {
        var options = new EFOptions();
        builder.Configuration.GetSection(TrainSolverOptions.SectionName).Bind(options);
        configureOptions?.Invoke(options);

        if (string.IsNullOrEmpty(options.DatabaseConnectionString))
        {
            throw new InvalidOperationException("Database connection string is not set.");
        }

        builder.Services.AddDbContext<SolverDbContext>(dbOptions =>
        {
            dbOptions.UseNpgsql(options.DatabaseConnectionString);

            if (options.DisableDatabaseLogging)
            {
                dbOptions.UseLoggerFactory(LoggerFactory.Create(builder => { }));
                dbOptions.EnableSensitiveDataLogging(false);
                dbOptions.EnableDetailedErrors(false);
            }
        });

        builder.Services.AddTransient<INetworkRepository, EFNetworkRepository>();
        builder.Services.AddTransient<IFeeRepository, EFFeeRepository>();
        builder.Services.AddTransient<ISwapRepository, EFSwapRepository>();
        builder.Services.AddTransient<IRouteRepository, EFRouteRepository>();
        builder.Services.AddTransient<IWalletRepository, EFWalletRepository>();
        builder.Services.AddTransient<ITrustedWalletRepository, EFTrustedWalletRepository>();
        builder.Services.AddTransient<ITokenPriceRepository, EFTokenPriceRepository>();

        if (options.MigrateDatabase)
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SolverDbContext>();
            dbContext.Database.Migrate();
        }

        return builder;
    }
}