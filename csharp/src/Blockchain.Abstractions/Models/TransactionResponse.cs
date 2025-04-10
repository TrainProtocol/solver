using ProtoBuf;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class TransactionResponse
{
    [ProtoMember(1)]
    public decimal Amount { get; set; }

    [ProtoMember(2)]
    public string Asset { get; set; } = null!;

    [ProtoMember(3)]
    public required string NetworkName { get; set; } = null!;

    [ProtoMember(4)]
    public required string TransactionHash { get; set; } = null!;

    [ProtoMember(5)]
    public required int Confirmations { get; set; }

    [ProtoMember(6)]
    public required DateTimeOffset Timestamp { get; set; }

    [ProtoMember(7)]
    public required decimal FeeAmount { get; set; }

    [ProtoMember(8)]
    public required string FeeAsset { get; set; }

    [ProtoMember(9)]
    public required TransactionStatus Status { get; set; }
}
