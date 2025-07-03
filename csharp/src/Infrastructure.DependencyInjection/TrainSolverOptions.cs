namespace Train.Solver.Infrastructure.DependencyInjection;

public class TrainSolverOptions
{
    public const string SectionName = "TrainSolver";

    public string TemporalServerHost { get; set; } = null!;

    public string TemporalNamespace { get; set; } = "prepare";

    public string RedisConnectionString { get; set; } = null!;

    public int RedisDatabaseIndex { get; set; } = 3;

    public bool DisableExpenseFee { get; set; } = false;
}
