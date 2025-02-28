using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Train.Solver.Data.Migrations;

public class SolverDbContextDesignFactory : IDesignTimeDbContextFactory<SolverDbContext>
{
    public SolverDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("dbsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SolverDbContext>();
        optionsBuilder.UseNpgsql(
            configuration.GetConnectionString("Layerswap"),
            x => x.MigrationsAssembly(GetType().Assembly.GetName().Name));

        return new SolverDbContext(optionsBuilder.Options);
    }
}
