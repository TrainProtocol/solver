namespace Train.Solver.Infrastrucutre.Secret.Treasury.Options;

public class TreasuryOptions
{
    public Uri TreasuryUrl { get; set; } = null!;

    public TimeSpan TreasuryTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
