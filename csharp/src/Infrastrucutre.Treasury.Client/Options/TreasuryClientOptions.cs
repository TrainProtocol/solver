namespace Train.Solver.Infrastructure.Treasury.Client.Options;

public class TreasuryClientOptions
{
    public Uri TreasuryUri { get; set; } = null!;

    public TimeSpan TreasuryTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
