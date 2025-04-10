using ProtoBuf;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class TransactionRequest : BaseRequest
{
    [ProtoMember(1)]
    public required string PrepareArgs { get; set; } = null!;

    [ProtoMember(2)]
    public required TransactionType Type { get; set; }

    [ProtoMember(3)]
    public required NetworkType NetworkType { get; set; }

    [ProtoMember(4)]
    public required string FromAddress { get; set; } = null!;

    [ProtoMember(5)]
    public required string SwapId { get; set; }
}
