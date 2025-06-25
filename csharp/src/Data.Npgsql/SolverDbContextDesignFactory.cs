using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Train.Solver.Infrastructure.DependencyInjection;

namespace Train.Solver.Data.Npgsql;

public class SolverDbContextDesignFactory : IDesignTimeDbContextFactory<SolverDbContext>
{
    public SolverDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("dbsettings.json", optional: false)
            .AddJsonFile("dbsettings.Local.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SolverDbContext>();

        var configurationOptions = configuration.GetSection(TrainSolverOptions.SectionName).Get<EFOptions>();

        optionsBuilder.UseNpgsql(
            configurationOptions!.DatabaseConnectionString,
            x => x.MigrationsAssembly(GetType().Assembly.GetName().Name));

        return new SolverDbContext(optionsBuilder.Options);
    }
}
