using MessagePack;

namespace Train.Solver.Core.Blockchain.Models;

[MessagePackObject]
public class ApprovePrepareRequest 
{
    [Key(0)]
    public string SpenderAddress { get; set; } = null!;

    [Key(1)]
    public string Asset { get; set; } = null!;

    [Key(2)]
    public decimal Amount { get; set; }
}
