using System.Text.Json.Serialization;
using Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models.Converters;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

public class JsHTLCTransferBuilderBase
{
    public string CorrelationId { get; set; } = null!;

    public string ReferenceId { get; set; } = null!;

    public string ContractAddress { get; set; }

    public bool IsErc20 { get; set; }

    [JsonConverter(typeof(StringEnumToLowerConverter))]
    public FunctionName FunctionName { get; set; }
}

public enum FunctionName
{
    Lock,
    Refund,
    Redeem,
    AddLockSig,
    Approve,
}


public class JsHTLCLockTransactionBuilderRequest : JsHTLCTransferBuilderBase
{
    public decimal Amount { get; set; }

    public string AmountInWei { get; set; }

    public decimal Reward { get; set; }

    public string RewardInWei { get; set; }

    public string RewardTimelock { get; set; }

    public string Hashlock { get; set; } = null!;

    public string Timelock { get; set; } = null!;

    public string Receiver { get; set; } = null!;

    public string SourceAsset { get; set; }

    public string DestinationChain { get; set; }

    public string DestinationAddress { get; set; }

    public string DestinationAsset { get; set; }

    public string Id { get; set; }

    public string TokenContract { get; set; }
}
