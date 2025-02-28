namespace Train.Solver.Core.DependencyInjection;

public class TrainSolverOptions
{
    public string TemporalServerHost { get; set; } = null!;

    public string TemporalNamespace { get; set; } = "atomic";

    public string TemporalTaskQueue { get; set; } = "atomic";

    public string DatabaseConnectionString { get; set; } = null!;

    public string RedisConnectionString { get; set; } = null!;

    public int RedisDatabaseIndex { get; set; } = 3;
}
