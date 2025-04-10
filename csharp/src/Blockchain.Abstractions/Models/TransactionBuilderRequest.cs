using ProtoBuf;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class TransactionBuilderRequest : BaseRequest
{
    [ProtoMember(1)]
    public TransactionType Type { get; set; }

    [ProtoMember(2)]
    public string Args { get; set; } = null!;
}
