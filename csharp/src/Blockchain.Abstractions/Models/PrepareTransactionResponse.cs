using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class PrepareTransactionResponse
{
    [ProtoMember(1)]
    public string ToAddress { get; set; } = null!;

    [ProtoMember(2)]
    public string? Data { get; set; }

    [ProtoMember(3)]
    public decimal Amount { get; set; }

    [ProtoMember(4)]
    public string Asset { get; set; } = null!;

    [ProtoMember(5)]
    public string AmountInWei { get; set; } = null!;

    [ProtoMember(6)]
    public string CallDataAsset { get; set; } = null!;

    [ProtoMember(7)]
    public string CallDataAmountInWei { get; set; } = null!;

    [ProtoMember(8)]
    public decimal CallDataAmount { get; set; }
}
