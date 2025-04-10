using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

//[ProtoContract]
public class EstimateFeeRequest : BaseRequest
{
    [ProtoMember(2)]
    public required string Asset { get; set; } = null!;

    [ProtoMember(3)]
    public required string FromAddress { get; set; } = null!;

    [ProtoMember(4)]
    public required string ToAddress { get; set; } = null!;

    [ProtoMember(5)]
    public required decimal Amount { get; set; }

    [ProtoMember(6)]
    public string? CallData { get; set; }
}
