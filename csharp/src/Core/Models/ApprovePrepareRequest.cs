using MessagePack;

namespace Train.Solver.Core.Models;

[MessagePackObject]
public class ApprovePrepareRequest 
{
    [Key(0)]
    public required string SpenderAddress { get; set; } = null!;

    [Key(1)]
    public required string Asset { get; set; } = null!;

    [Key(2)]
    public required decimal Amount { get; set; }
}
