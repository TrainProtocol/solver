using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
public class TransactionExecutionContext
{
    [ProtoMember(1)]
    public int Attempts { get; set; } = 1;

    [ProtoMember(2)]
    public Fee? Fee { get; set; }

    [ProtoMember(3)]
    public string? Nonce { get; set; }

    [ProtoMember(4)]
    public HashSet<string> PublishedTransactionIds { get; set; } = new();
}
