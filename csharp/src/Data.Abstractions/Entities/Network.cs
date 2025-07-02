using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public enum NetworkType
{
    EVM,
    Solana,
    Starknet,
    Fuel,
}

public enum TransactionFeeType
{
    Default,
    EIP1559,
    ArbitrumEIP1559,
    OptimismEIP1559,
}

public class Network : EntityBase<int>
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public NetworkType Type { get; set; }

    public TransactionFeeType FeeType { get; set; }

    public string ChainId { get; set; } = null!;

    public int FeePercentageIncrease { get; set; }

    public string HTLCNativeContractAddress { get; set; } = null!;

    public string HTLCTokenContractAddress { get; set; } = null!;

    public int? NativeTokenId { get; set; }

    public virtual Token? NativeToken { get; set; } = null!;

    public virtual List<Token> Tokens { get; set; } = [];

    public virtual List<Node> Nodes { get; set; } = [];
}
