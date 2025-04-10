using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;


[ProtoContract]
public class GetBatchTransactionRequest : BaseRequest
{
    [ProtoMember(1)]
    public string[] TransactionHashes { get; set; } = null!;
}
