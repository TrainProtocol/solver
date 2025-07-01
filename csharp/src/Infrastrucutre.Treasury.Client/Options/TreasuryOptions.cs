namespace Train.Solver.Infrastructure.Treasury.Client.Options;

public class TreasuryOptions
{
    public Uri TreasuryUrl { get; set; } = null!;

    public TimeSpan TreasuryTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
