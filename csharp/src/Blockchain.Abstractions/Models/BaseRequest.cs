using ProtoBuf;

namespace Train.Solver.Blockchain.Abstractions.Models;

[ProtoContract]
[ProtoInclude(1000, typeof(AllowanceRequest))]
[ProtoInclude(2000, typeof(BalanceRequest))]
//[ProtoInclude(3000, typeof(EstimateFeeRequest))]
[ProtoInclude(4000, typeof(EventRequest))]
[ProtoInclude(5000, typeof(GetBatchTransactionRequest))]
[ProtoInclude(6000, typeof(TransactionRequest))]
[ProtoInclude(7000, typeof(TransactionBuilderRequest))]
[ProtoInclude(8000, typeof(SpenderAddressRequest))]
[ProtoInclude(9000, typeof(NextNonceRequest))]
public class BaseRequest
{
    [ProtoMember(1)]
    public required string NetworkName { get; set; } = null!;
}
