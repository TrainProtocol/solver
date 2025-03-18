using MessagePack;

namespace Train.Solver.Core.Models;

[MessagePackObject]
public class TransferPrepareRequest 
{
    [Key(0)]
    public string ToAddress { get; set; } = null!;

    [Key(1)]
    public string Asset { get; set; } = null!;

    [Key(2)]
    public decimal Amount { get; set; }

    [Key(3)]
    public string? Memo { get; set; }

    [Key(4)]
    public string? FromAddress { get; set; }
}