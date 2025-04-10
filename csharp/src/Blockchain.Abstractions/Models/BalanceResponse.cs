using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class BalanceResponse
{
    [ProtoMember(1)]
    public string AmountInWei { get; set; } = null!;

    [ProtoMember(2)]
    public decimal Amount { get; set; }

    [ProtoMember(3)]
    public int Decimals { get; set; }
}
