namespace Train.Solver.Core.Models;

public class AddLockSigRequest
{
    public string? R { get; set; }

    public string? S { get; set; }

    public string? V { get; set; }

    public string? Signature { get; set; }

    public string[]? SignatureArray { get; set; }

    public long Timelock { get; set; }
}
