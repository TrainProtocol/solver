using MessagePack;

namespace Train.Solver.Core.Models;


[MessagePackObject]
public class HTLCAddLockSigTransactionPrepareRequest
{
    [Key(0)]
    public string Id { get; set; } = null!;

    [Key(1)]
    public string Hashlock { get; set; } = null!;

    [Key(2)]
    public long Timelock { get; set; }

    [Key(3)]
    public string? R { get; set; }

    [Key(4)]
    public string? S { get; set; }

    [Key(5)]
    public string? V { get; set; }

    [Key(6)]
    public string? Signature { get; set; }

    [Key(7)]
    public string Asset { get; set; } = null!;

    [Key(8)]
    public string[]? SignatureArray { get; set; }
}
