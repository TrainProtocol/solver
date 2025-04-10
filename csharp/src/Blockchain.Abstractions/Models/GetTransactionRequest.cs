using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class GetTransactionRequest : BaseRequest
{
    [ProtoMember(1)]
    public required string TransactionHash { get; set; } = null!;
}
