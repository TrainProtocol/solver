namespace Train.Solver.Data.Npgsql;

public class EFOptions
{
    public string DatabaseConnectionString { get; set; } = null!;

    public bool DisableDatabaseLogging { get; set; } = false;

    public bool MigrateDatabase { get; set; }
}
