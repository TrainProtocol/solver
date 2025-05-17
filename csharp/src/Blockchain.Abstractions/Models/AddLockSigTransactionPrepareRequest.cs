namespace Train.Solver.Blockchain.Abstractions.Models;

public class AddLockSigTransactionPrepareRequest
{
    public string Id { get; set; } = null!;

    public string Hashlock { get; set; } = null!;

    public long Timelock { get; set; }

    public string? R { get; set; }

    public string? S { get; set; }

    public string? V { get; set; }

    public string? Signature { get; set; }

    public string Asset { get; set; } = null!;

    public string[]? SignatureArray { get; set; }

    public string? SignerAddress { get; set; }
}
